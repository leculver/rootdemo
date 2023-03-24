using Microsoft.Diagnostics.Runtime.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HeapEnumerationTests
{

    internal unsafe class ICorDebugReferenceValue : CallableCOMWrapper
    {
        public static readonly Guid IID_ICorDebugReferenceValue = new("CC7BCAF9-8A68-11d2-983C-0000F808342D");
        private ref readonly ICorDebugReferenceValueVtable VTable => ref Unsafe.AsRef<ICorDebugReferenceValueVtable>(_vtable);
        public ICorDebugReferenceValue(nint ptr)
            : base(null, IID_ICorDebugReferenceValue, ptr)
        {
        }

        [StructLayout(LayoutKind.Sequential)]
        private readonly unsafe struct ICorDebugReferenceValueVtable
        {
            private readonly nint GetType2;
            private readonly nint GetSize;
            public readonly delegate* unmanaged[Stdcall]<IntPtr, out ulong, int> GetAddress;
        }
    }
}
