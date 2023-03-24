using Microsoft.Diagnostics.Runtime.Utilities;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Runtime;

namespace HeapEnumerationTests
{
    public sealed unsafe class CorDebugDataTargetWrapper : COMCallableIUnknown
    {
        private static readonly Guid IID_ICorDebugDataTarget = new("FE06DC28-49FB-4636-A4A3-E80DB4AE116C");
        private static readonly Guid IID_ICorDebugDataTarget4 = new("E799DC06-E099-4713-BDD9-906D3CC02CF2");
        private static readonly Guid IID_ICorDebugMutableDataTarget = new("A1B8A756-3CB6-4CCB-979F-3DF999673A59");
        private static readonly Guid IID_ICorDebugMetaDataLocator = new("7cef8ba9-2ef7-42bf-973f-4171474f87d9");
        private readonly ulong _ignoreAddressBitsMask;
        private readonly DataTarget _dataTarget;
        private readonly IDataReader _dataReader;

        public IntPtr ICorDebugDataTarget { get; }

        public CorDebugDataTargetWrapper(DataTarget dataTarget)
        {
            _ignoreAddressBitsMask = dataTarget.DataReader.PointerSize == 4 ? uint.MaxValue : ulong.MaxValue;

            _dataTarget = dataTarget;
            _dataReader = dataTarget.DataReader;
            VTableBuilder builder = AddInterface(IID_ICorDebugDataTarget, validate: false);
            builder.AddMethod(new GetPlatformDelegate(GetPlatform));
            builder.AddMethod(new ReadVirtualDelegate(ReadVirtual));
            builder.AddMethod(new GetThreadContextDelegate(GetThreadContext));
            ICorDebugDataTarget = builder.Complete();

            builder = AddInterface(IID_ICorDebugMetaDataLocator, validate: false);
            builder.AddMethod(new GetMetaDataDelegate(GetMetaData));
            builder.Complete();

            AddRef();
        }

        protected override void Destroy()
        {
            Trace.TraceInformation("CorDebugDataTargetWrapper.Destroy");
        }

        #region ICorDebugDataTarget

        private int GetPlatform(
            IntPtr self,
            out CorDebugPlatform platform)
        {
            platform = CorDebugPlatform.CORDB_PLATFORM_WINDOWS_AMD64;
            OSPlatform osPlatform = _dataTarget.DataReader.TargetPlatform;
            Architecture architecture = _dataTarget.DataReader.Architecture;

            if (osPlatform == OSPlatform.Windows)
            {
                switch (architecture)
                {
                    case Architecture.X64:
                        platform = CorDebugPlatform.CORDB_PLATFORM_WINDOWS_AMD64;
                        break;
                    case Architecture.X86:
                        platform = CorDebugPlatform.CORDB_PLATFORM_WINDOWS_X86;
                        break;
                    case Architecture.Arm:
                        platform = CorDebugPlatform.CORDB_PLATFORM_WINDOWS_ARM;
                        break;
                    case Architecture.Arm64:
                        platform = CorDebugPlatform.CORDB_PLATFORM_WINDOWS_ARM64;
                        break;
                    default:
                        return HResult.E_FAIL;
                }
            }
            else if (osPlatform == OSPlatform.Linux || osPlatform == OSPlatform.OSX)
            {
                switch (architecture)
                {
                    case Architecture.X64:
                        platform = CorDebugPlatform.CORDB_PLATFORM_POSIX_AMD64;
                        break;
                    case Architecture.X86:
                        platform = CorDebugPlatform.CORDB_PLATFORM_POSIX_X86;
                        break;
                    case Architecture.Arm:
                        platform = CorDebugPlatform.CORDB_PLATFORM_POSIX_ARM;
                        break;
                    case Architecture.Arm64:
                        platform = CorDebugPlatform.CORDB_PLATFORM_POSIX_ARM64;
                        break;
                    default:
                        return HResult.E_FAIL;
                }
            }
            else
            {
                return HResult.E_FAIL;
            }
            return HResult.S_OK;
        }

        private unsafe int ReadVirtual(
            IntPtr self,
            ulong address,
            IntPtr buffer,
            uint bytesRequested,
            uint* pbytesRead)
        {
            if (bytesRequested > 0)
            {
                address &= _ignoreAddressBitsMask;

                Span<byte> bytes = new(buffer.ToPointer(), (int)bytesRequested);
                uint read = (uint)_dataReader.Read(address, bytes);
                *pbytesRead = read;

                return read > 0 ? HResult.S_OK : HResult.E_FAIL;
            }

            return HResult.S_OK;
        }

        private int GetThreadContext(
            IntPtr self,
            uint threadId,
            uint contextFlags,
            uint contextSize,
            IntPtr context)
        {
            if (_dataReader.GetThreadContext(threadId, contextFlags, new(context.ToPointer(), (int)contextSize)))
                return HResult.S_OK;

            return HResult.E_FAIL;
        }

        #endregion

        #region ICorDebugMetaDataLocator

        private int GetMetaData(
            IntPtr self,
            string imagePath,
            uint imageTimestamp,
            uint imageSize,
            uint pathBufferSize,
            IntPtr pPathBufferSize,
            IntPtr pPathBuffer)
        {
            string? path = _dataTarget.FileLocator?.FindPEImage(imagePath, (int)imageTimestamp, (int)imageSize, checkProperties: false);
            if (path is not null && path.Length < pPathBufferSize.ToInt64())
            {
                Marshal.Copy(path.ToCharArray(), 0, pPathBuffer, path.Length);
                return HResult.S_OK;
            }

            return HResult.E_FAIL;
        }

        #endregion

        #region ICorDebugDataTarget delegates

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate int GetPlatformDelegate(
            [In] IntPtr self,
            [Out] out CorDebugPlatform platform);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate int ReadVirtualDelegate(
            [In] IntPtr self,
            [In] ulong address,
            [Out] IntPtr buffer,
            [In] uint bytesRequested,
            [Out] uint* pbytesRead);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate int GetThreadContextDelegate(
            [In] IntPtr self,
            [In] uint threadId,
            [In] uint contextFlags,
            [In] uint contextSize,
            [Out] IntPtr context);

        #endregion

        #region ICorDebugDataTarget4 delegates

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate int VirtualUnwindDelegate(
            [In] IntPtr self,
            [In] uint threadId,
            [In] uint contextSize,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] context);

        #endregion

        #region ICorDebugMutableDataTarget delegates

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate int WriteVirtualDelegate(
            [In] IntPtr self,
            [In] ulong address,
            [In] IntPtr buffer,
            [In] uint bytesRequested);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate int SetThreadContextDelegate(
            [In] IntPtr self,
            [In] uint threadId,
            [In] uint contextSize,
            [In] IntPtr context);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate int ContinueStatusChangeDelegate(
            [In] IntPtr self,
            [In] uint continueStatus);

        #endregion

        #region ICorDebugMetaDataLocator delegates

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate int GetMetaDataDelegate(
            [In] IntPtr self,
            [In, MarshalAs(UnmanagedType.LPWStr)] string imagePath,
            [In] uint imageTimestamp,
            [In] uint imageSize,
            [In] uint pathBufferSize,
            [Out] IntPtr pPathBufferSize,
            [Out] IntPtr pPathBuffer);

        #endregion
    }
    public enum CorDebugPlatform
    {
        CORDB_PLATFORM_WINDOWS_X86 = 0, // Windows on Intel x86
        CORDB_PLATFORM_WINDOWS_AMD64 = 1, // Windows x64 (Amd64, Intel EM64T)
        CORDB_PLATFORM_WINDOWS_IA64 = 2, // Windows on Intel IA-64
        CORDB_PLATFORM_MAC_PPC = 3, // MacOS on PowerPC
        CORDB_PLATFORM_MAC_X86 = 4, // MacOS on Intel x86
        CORDB_PLATFORM_WINDOWS_ARM = 5, // Windows on ARM
        CORDB_PLATFORM_MAC_AMD64 = 6,
        CORDB_PLATFORM_WINDOWS_ARM64 = 7, // Windows on ARM64
        CORDB_PLATFORM_POSIX_AMD64 = 8,
        CORDB_PLATFORM_POSIX_X86 = 9,
        CORDB_PLATFORM_POSIX_ARM = 10,
        CORDB_PLATFORM_POSIX_ARM64 = 11
    }
}
