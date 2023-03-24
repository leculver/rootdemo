namespace HeapEnumerationTests
{
    enum CorGCReferenceType : uint
    {
        CorHandleStrong = 1 << 0,
        CorHandleStrongPinning = 1 << 1,
        CorHandleWeakShort = 1 << 2,
        CorHandleWeakLong = 1 << 3,
        CorHandleWeakRefCount = 1 << 4,
        CorHandleStrongRefCount = 1 << 5,
        CorHandleStrongDependent = 1 << 6,
        CorHandleStrongAsyncPinned = 1 << 7,
        CorHandleStrongSizedByref = 1 << 8,
        CorHandleWeakNativeCom = 1 << 9,
        CorHandleWeakWinRT = CorHandleWeakNativeCom,

        CorReferenceStack = 0x80000001,
        CorReferenceFinalizer = 80000002,

        // Used for EnumHandles
        CorHandleStrongOnly = 0x1E3,
        CorHandleWeakOnly = 0x21C,
        CorHandleAll = 0x7FFFFFFF
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