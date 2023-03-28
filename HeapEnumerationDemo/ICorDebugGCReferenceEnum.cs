using Microsoft.Diagnostics.Runtime.Utilities;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HeapEnumerationTests
{
    internal unsafe class ICorDebugGCReferenceEnum : CallableCOMWrapper
    {
        public static readonly Guid IID_ICorDebugGCReferenceEnum = new("7F3C24D3-7E1D-4245-AC3A-F72F8859C80C");
        private ref readonly ICorDebugGCReferenceEnumVtable VTable => ref Unsafe.AsRef<ICorDebugGCReferenceEnumVtable>(_vtable);

        public ICorDebugGCReferenceEnum(nint ptr)
            : base(null, IID_ICorDebugGCReferenceEnum, ptr)
        {
        }

        public IEnumerable<COR_GC_REFERENCE> EnumerateReferences()
        {
            COR_GC_REFERENCE[] buffer = new COR_GC_REFERENCE[128];
            for (int count = ReadMore(buffer); count > 0; count = ReadMore(buffer))
            {
                for (int i = 0; i < buffer.Length; i++)
                    yield return buffer[i];
            }
        }

        private int ReadMore(COR_GC_REFERENCE[] buffer)
        {
            fixed (COR_GC_REFERENCE* ptr = buffer)
            {
                if (VTable.Next(Self, buffer.Length, ptr, out int read) < 0)
                    return 0;

                return read;
            }

        }

        public ICorDebugGCReferenceEnum2? GCReferenceEnum2
        {
            get
            {
                try
                {
                    return new ICorDebugGCReferenceEnum2(Self);
                }
                catch (InvalidCastException)
                {
                    return null;
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private readonly unsafe struct ICorDebugGCReferenceEnumVtable
        {

            private readonly nint Skip;
            private readonly nint Reset;
            private readonly nint Clone;
            private readonly nint GetCount;
            public readonly delegate* unmanaged[Stdcall]<IntPtr, int, COR_GC_REFERENCE*, out int, int> Next;
        }
    }
}
