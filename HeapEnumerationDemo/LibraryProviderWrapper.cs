using Microsoft.Diagnostics.Runtime.Utilities;
using Microsoft.Diagnostics.Runtime;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HeapEnumerationTests
{

    public sealed unsafe class LibraryProviderWrapper : COMCallableIUnknown
    {
        public enum LIBRARY_PROVIDER_INDEX_TYPE
        {
            Unknown = 0,
            Identity = 1,
            Runtime = 2,
        };

        public static readonly Guid IID_ICLRDebuggingLibraryProvider = new("3151C08D-4D09-4f9b-8838-2880BF18FE51");
        public static readonly Guid IID_ICLRDebuggingLibraryProvider2 = new("E04E2FF1-DCFD-45D5-BCD1-16FFF2FAF7BA");
        public static readonly Guid IID_ICLRDebuggingLibraryProvider3 = new("DE3AAB18-46A0-48B4-BF0D-2C336E69EA1B");

        public IntPtr ILibraryProvider { get; }

        private readonly OSPlatform _targetOS;
        private readonly ImmutableArray<byte> _runtimeModuleBuildId;
        private readonly string _dbiModulePath;
        private readonly string _dacModulePath;

        public LibraryProviderWrapper(ClrInfo clrInfo)
        {
            _targetOS = clrInfo.DataTarget.DataReader.TargetPlatform;
            _runtimeModuleBuildId = clrInfo.BuildId;

            IFileLocator locator = clrInfo.DataTarget.FileLocator ?? throw new ArgumentException("clrInfo must have a valid FileLocator");
            IEnumerable<DebugLibraryInfo> matchingLibraries = clrInfo.DebuggingLibraries.Where(d => d.TargetArchitecture == GetArchitecture() && RuntimeInformation.IsOSPlatform(d.Platform));

            _dacModulePath = GetMatchingLibrary(locator, matchingLibraries, DebugLibraryKind.Dac);
            string potentialDbi = Path.Combine(Path.GetDirectoryName(_dacModulePath)??"", "mscordbi.dll");
            if (File.Exists(potentialDbi))
                _dbiModulePath = potentialDbi;
            else
                _dbiModulePath = GetMatchingLibrary(locator, matchingLibraries, DebugLibraryKind.Dbi);

            VTableBuilder builder = AddInterface(IID_ICLRDebuggingLibraryProvider, validate: false);
            builder.AddMethod(new ProvideLibraryDelegate(ProvideLibrary));
            ILibraryProvider = builder.Complete();

            builder = AddInterface(IID_ICLRDebuggingLibraryProvider2, validate: false);
            builder.AddMethod(new ProvideLibrary2Delegate(ProvideLibrary2));
            builder.Complete();

            builder = AddInterface(IID_ICLRDebuggingLibraryProvider3, validate: false);
            builder.AddMethod(new ProvideWindowsLibraryDelegate(ProvideWindowsLibrary));
            builder.AddMethod(new ProvideUnixLibraryDelegate(ProvideUnixLibrary));
            builder.Complete();

            AddRef();
        }

        private static string GetMatchingLibrary(IFileLocator locator,  IEnumerable<DebugLibraryInfo> matchingLibraries, DebugLibraryKind kind)
        {
            foreach (DebugLibraryInfo lib in matchingLibraries.Where(d => d.Kind == kind))
            {
                if (File.Exists(lib.FileName))
                    return lib.FileName;

                string? found = locator.FindPEImage(lib.FileName, lib.IndexTimeStamp, lib.IndexFileSize, checkProperties: false);

                if (found is not null)
                    return found;
            }

            throw new IOException();
        }

        private static Architecture GetArchitecture()
        {
            return Environment.Is64BitProcess ? Architecture.X64 : Architecture.X86;
        }

        private static OSPlatform GetRunningOS()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return OSPlatform.Windows;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return OSPlatform.Linux;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return OSPlatform.OSX;
            }

            throw new NotSupportedException($"OS not supported {RuntimeInformation.OSDescription}");
        }

        protected override void Destroy()
        {
            Trace.TraceInformation("LibraryProviderWrapper.Destroy");
        }

        private int ProvideLibrary(
            IntPtr self,
            string fileName,
            uint timeStamp,
            uint sizeOfImage,
            out IntPtr moduleHandle)
        {

            if (Path.GetFileNameWithoutExtension(fileName).Contains("dbi"))
            {
                moduleHandle = DataTarget.PlatformFunctions.LoadLibrary(_dbiModulePath);
                return HResult.S_OK;
            }


            if (Path.GetFileNameWithoutExtension(fileName).Contains("dac"))
            {
                moduleHandle = DataTarget.PlatformFunctions.LoadLibrary(_dacModulePath);
                return HResult.S_OK;
            }

            moduleHandle = 0;
            return HResult.E_FAIL;
        }

        private int ProvideLibrary2(
            IntPtr self,
            string fileName,
            uint timeStamp,
            uint sizeOfImage,
            out IntPtr modulePathOut)
        {
            if (Path.GetFileNameWithoutExtension(fileName).Contains("dbi"))
            {
                modulePathOut = Marshal.StringToCoTaskMemUni(_dbiModulePath);
                return HResult.S_OK;
            }

            if (Path.GetFileNameWithoutExtension(fileName).Contains("dac"))
            {
                modulePathOut = Marshal.StringToCoTaskMemUni(_dacModulePath);
                return HResult.S_OK;
            }

            modulePathOut = 0;
            return HResult.E_FAIL;
        }

        private int ProvideWindowsLibrary(
            IntPtr self,
            string fileName,
            string runtimeModulePath,
            LIBRARY_PROVIDER_INDEX_TYPE indexType,
            uint timeStamp,
            uint sizeOfImage,
            out IntPtr modulePathOut)
        {
            if (Path.GetFileNameWithoutExtension(fileName).Contains("dbi"))
            {
                modulePathOut = Marshal.StringToCoTaskMemUni(_dbiModulePath);
                return HResult.S_OK;
            }

            if (Path.GetFileNameWithoutExtension(fileName).Contains("dac"))
            {
                modulePathOut = Marshal.StringToCoTaskMemUni(_dacModulePath);
                return HResult.S_OK;
            }

            modulePathOut = 0;
            return HResult.E_FAIL;
        }

        private int ProvideUnixLibrary(
            IntPtr self,
            string fileName,
            string runtimeModulePath,
            LIBRARY_PROVIDER_INDEX_TYPE indexType,
            byte* buildIdBytes,
            int buildIdSize,
            out IntPtr modulePathOut)
        {
            modulePathOut = IntPtr.Zero;
            return HResult.E_NOTIMPL;
        }


        #region ICLRDebuggingLibraryProvider* delegates

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate int ProvideLibraryDelegate(
            [In] IntPtr self,
            [In, MarshalAs(UnmanagedType.LPWStr)] string fileName,
            [In] uint timeStamp,
            [In] uint sizeOfImage,
            out IntPtr moduleHandle);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate int ProvideLibrary2Delegate(
            [In] IntPtr self,
            [In, MarshalAs(UnmanagedType.LPWStr)] string fileName,
            [In] uint timeStamp,
            [In] uint sizeOfImage,
            out IntPtr modulePath);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate int ProvideWindowsLibraryDelegate(
            [In] IntPtr self,
            [In, MarshalAs(UnmanagedType.LPWStr)] string fileName,
            [In, MarshalAs(UnmanagedType.LPWStr)] string runtimeModulePath,
            [In] LIBRARY_PROVIDER_INDEX_TYPE indexType,
            [In] uint timeStamp,
            [In] uint sizeOfImage,
            out IntPtr modulePath);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate int ProvideUnixLibraryDelegate(
            [In] IntPtr self,
            [In, MarshalAs(UnmanagedType.LPWStr)] string fileName,
            [In, MarshalAs(UnmanagedType.LPWStr)] string runtimeModulePath,
            [In] LIBRARY_PROVIDER_INDEX_TYPE indexType,
            [In] byte* buildIdBytes,
            [In] int buildIdSize,
            out IntPtr modulePath);

        #endregion
    }
}
