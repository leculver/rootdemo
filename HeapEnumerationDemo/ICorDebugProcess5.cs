using Microsoft.Diagnostics.Runtime.Utilities;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace HeapEnumerationTests
{
    internal unsafe class ICorDebugProcess5 : CallableCOMWrapper
    {
        public static readonly Guid IID_ICorDebugProcess5 = new("21e9d9c0-fcb8-11df-8cff-0800200c9a66");
        private ref readonly ICorDebugProcess5Vtable VTable => ref Unsafe.AsRef<ICorDebugProcess5Vtable>(_vtable);

        public ICorDebugProcess5(nint ptr)
            : base(null, IID_ICorDebugProcess5, ptr)
        {
        }

        public ICorDebugGCReferenceEnum? EnumerateGCReferences(bool includeWeak)
        {
            HResult hr = VTable.EnumerateGCReferences(Self, includeWeak ? 1 : 0, out nint result);

            if (hr && result != 0)
                return new(result);

            return null;
        }

        [StructLayout(LayoutKind.Sequential)]
        private readonly unsafe struct ICorDebugProcess5Vtable
        {
            private readonly nint GetGCHeapInformation;
            private readonly nint EnumerateHeap;
            private readonly nint EnumerateHeapRegions;
            private readonly nint GetObject;
            public readonly delegate* unmanaged[Stdcall]<IntPtr, int, out nint, int> EnumerateGCReferences;
            private readonly nint EnumerateHandles;
        }
    }
}

/*   
HRESULT GetGCHeapInformation([out] COR_HEAPINFO *pHeapInfo);
HRESULT EnumerateHeap([out] ICorDebugHeapEnum **ppObjects);
HRESULT EnumerateHeapRegions([out] ICorDebugHeapSegmentEnum **ppRegions);
HRESULT GetObject([in] CORDB_ADDRESS addr, [out] ICorDebugObjectValue **pObject);
HRESULT EnumerateGCReferences([in] BOOL enumerateWeakReferences, [out] ICorDebugGCReferenceEnum **ppEnum);
HRESULT EnumerateHandles([in] CorGCReferenceType types, [out] ICorDebugGCReferenceEnum **ppEnum);

HRESULT GetTypeID([in] CORDB_ADDRESS obj, [out] COR_TYPEID *pId);
HRESULT GetTypeForTypeID([in] COR_TYPEID id, [out] ICorDebugType **ppType);

HRESULT GetArrayLayout([in] COR_TYPEID id, [out] COR_ARRAY_LAYOUT *pLayout);
HRESULT GetTypeLayout([in] COR_TYPEID id, [out] COR_TYPE_LAYOUT *pLayout);
HRESULT GetTypeFields([in] COR_TYPEID id, ULONG32 celt, COR_FIELD fields[], ULONG32 *pceltNeeded);
*/