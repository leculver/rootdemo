using Microsoft.Diagnostics.Runtime.Implementation;
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
    internal unsafe class ICorDebugValue : CallableCOMWrapper
    {
        public static readonly Guid IID_ICorDebugValue = new("CC7BCAF7-8A68-11d2-983C-0000F808342D");
        private ref readonly ICorDebugValueVtable VTable => ref Unsafe.AsRef<ICorDebugValueVtable>(_vtable);
        public ICorDebugValue(nint ptr)
            : base(null, IID_ICorDebugValue, ptr)
        {
        }

        public ulong GetAddress()
        {
            HResult hr = VTable.GetAddress(Self, out ulong address);

            return address;
        }

        public bool IsReferenceValue
        {
            get
            {
                nint value = QueryInterface(ICorDebugReferenceValue.IID_ICorDebugReferenceValue);
                if (value == 0)
                    return false;

                return true;
            }
        }


        public bool IsObjectValue
        {
            get
            {
                nint value = QueryInterface(ICorDebugObjectValue.IID_ICorDebugObjectValue);
                if (value == 0)
                    return false;

                return true;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private readonly unsafe struct ICorDebugValueVtable
        {
            private readonly nint GetType2;
            private readonly nint GetSize;
            public readonly delegate* unmanaged[Stdcall]<IntPtr, out ulong, int> GetAddress;
        }
    }
}
