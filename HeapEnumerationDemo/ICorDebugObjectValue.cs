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
    internal unsafe class ICorDebugObjectValue : CallableCOMWrapper
    {
        public static readonly Guid IID_ICorDebugObjectValue = new("18AD3D6E-B7D2-11d2-BD04-0000F80849BD");
        private ref readonly ICorDebugObjectValueVtable VTable => ref Unsafe.AsRef<ICorDebugObjectValueVtable>(_vtable);
        public ICorDebugObjectValue(nint ptr)
            : base (null, IID_ICorDebugObjectValue, ptr)
        {
        }

        [StructLayout(LayoutKind.Sequential)]
        private readonly unsafe struct ICorDebugObjectValueVtable
        {
            private readonly nint GetType2;
            private readonly nint GetSize;
            public readonly delegate* unmanaged[Stdcall]<IntPtr, out ulong, int> GetAddress;
        }
    }
}
