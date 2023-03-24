using System.Runtime.InteropServices;

namespace HeapEnumerationTests
{
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct COR_GC_REFERENCE
    {
        public readonly nint Domain;
        public readonly nint Location;
        public readonly CorGCReferenceType Type;
        public readonly ulong ExtraData;


        public unsafe override string ToString()
        {
            if (Location == 0)
            {
                return "{0}";
            }

            using ICorDebugValue value = new(Location);
            string result = $"{Type} - {value.GetAddress():x}";

            if (value.IsObjectValue)
                result += " - [ObjectValue]";

            if (value.IsReferenceValue)
                result += " - [ReferenceValue]";

            if (ExtraData != 0)
                result += $" - ExtraData: {ExtraData:x}";

            return result;
        }
    }
}
