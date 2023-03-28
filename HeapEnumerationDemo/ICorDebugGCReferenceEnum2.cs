using Microsoft.Diagnostics.Runtime.Utilities;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HeapEnumerationTests
{
    internal unsafe class ICorDebugGCReferenceEnum2 : CallableCOMWrapper
    {
        public static readonly Guid IID_ICorDebugGCReferenceEnum2 = new("7F3C24D3-7E1D-4245-AC3A-0D8308D0B14C");
        private ref readonly ICorDebugGCReferenceEnum2Vtable VTable => ref Unsafe.AsRef<ICorDebugGCReferenceEnum2Vtable>(_vtable);

        public ICorDebugGCReferenceEnum2(nint ptr)
            : base(null, IID_ICorDebugGCReferenceEnum2, ptr)
        {
        }

        public int DisableInteriorPointerDecoding(bool disable)
        {
            return VTable.DisableInteriorPointerDecoding(Self, disable ? 1 : 0);
        }

        [StructLayout(LayoutKind.Sequential)]
        private readonly unsafe struct ICorDebugGCReferenceEnum2Vtable
        {

            private readonly nint Skip;
            private readonly nint Reset;
            private readonly nint Clone;
            private readonly nint GetCount;
            public readonly delegate* unmanaged[Stdcall]<IntPtr, int, COR_GC_REFERENCE*, out int, int> Next;
            public readonly delegate* unmanaged[Stdcall]<IntPtr, int, int> DisableInteriorPointerDecoding;
        }
    }
}
