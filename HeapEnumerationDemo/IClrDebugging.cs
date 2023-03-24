using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Utilities;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HeapEnumerationTests
{
    internal unsafe class ICLRDebugging : CallableCOMWrapper
    {
        public static readonly Guid IID_ICLRDebugging = new("D28F3C5A-9634-4206-A509-477552EEFB10");
        public static readonly Guid CLSID_ICLRDebugging = new("BACC578D-FBDD-48A4-969F-02D932B74634");
        private static nint _dbgshimModuleHandle;

        private ref readonly ICLRDebuggingVTable VTable => ref Unsafe.AsRef<ICLRDebuggingVTable>(_vtable);

        public static ICLRDebugging? Create(string dbgShimPath)
        {
            nint shimHandle = GetDbgShimHandle(dbgShimPath);
            nint pUnk = GetInstance(shimHandle);
            if (pUnk != 0)
                return new ICLRDebugging(new RefCountedFreeLibrary(shimHandle), pUnk);

            DataTarget.PlatformFunctions.FreeLibrary(shimHandle);
            return null;
        }

        private ICLRDebugging(RefCountedFreeLibrary? lib, nint ptr)
            : base(lib, IID_ICLRDebugging, ptr)
        {
        }

        private static nint GetInstance(nint module)
        {
            CLRCreateInstanceDelegate? clrCreateInstance = GetDelegateFunction<CLRCreateInstanceDelegate>(module, "CLRCreateInstance");
            if (clrCreateInstance is not null)
            {
                HResult hr = clrCreateInstance(CLSID_ICLRDebugging, IID_ICLRDebugging, out IntPtr pUnk);
                Debug.Assert(hr);

                return pUnk;
            }

            return 0;
        }

        private static nint GetDbgShimHandle(string dbgShimPath)
        {
            if (_dbgshimModuleHandle == 0)
                _dbgshimModuleHandle = DataTarget.PlatformFunctions.LoadLibrary(dbgShimPath);
            return _dbgshimModuleHandle;
        }

        static private T? GetDelegateFunction<T>(nint handle, string functionName, bool optional = false)
            where T : Delegate
        {
            IntPtr functionAddress = DataTarget.PlatformFunctions.GetLibraryExport(handle, functionName);
            if (functionAddress == IntPtr.Zero)
            {
                if (optional)
                {
                    return default;
                }
                throw new ArgumentException($"Failed to get address of {functionName}");
            }

            return Marshal.GetDelegateForFunctionPointer<T>(functionAddress);
        }

        public HResult OpenVirtualProcess(
            ulong moduleBaseAddress,
            IntPtr dataTarget,
            IntPtr libraryProvider,
            ClrDebuggingVersion maxDebuggerSupportedVersion,
            in Guid riidProcess,
            out IntPtr process,
            out ClrDebuggingVersion version,
            out ClrDebuggingProcessFlags flags)
        {
            return VTable.OpenVirtualProcess(
                Self,
                moduleBaseAddress,
                dataTarget,
                libraryProvider,
                in maxDebuggerSupportedVersion,
                in riidProcess,
                out process,
                out version,
                out flags);
        }

        public nint CreateICorDebugProcess(ClrInfo clr, Guid guid)
        {
            CorDebugDataTargetWrapper dataTarget = new(clr.DataTarget);

            LibraryProviderWrapper libraryProvider = new(clr);
            ClrDebuggingVersion maxDebuggerSupportedVersion = new()
            {
                StructVersion = 0,
                Major = 4,
                Minor = 0,
                Build = 0,
                Revision = 0,
            };
            HResult hr = OpenVirtualProcess(
                clr.ModuleInfo.ImageBase,
                dataTarget.ICorDebugDataTarget,
                libraryProvider.ILibraryProvider,
                maxDebuggerSupportedVersion,
                guid,
                out IntPtr corDebugProcess,
                out ClrDebuggingVersion version,
                out ClrDebuggingProcessFlags flags);

            return corDebugProcess;
        }


        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private unsafe delegate int CLRCreateInstanceDelegate(
            in Guid clrsid,
            in Guid riid,
            out IntPtr pInterface);


        [StructLayout(LayoutKind.Sequential)]
        private readonly unsafe struct ICLRDebuggingVTable
        {
            public readonly delegate* unmanaged[Stdcall]<IntPtr, ulong, IntPtr, IntPtr, in ClrDebuggingVersion, in Guid, out IntPtr, out ClrDebuggingVersion, out ClrDebuggingProcessFlags, int> OpenVirtualProcess;
        }
    }
}
