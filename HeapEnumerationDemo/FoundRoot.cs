using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeapEnumerationTests
{
    public class FoundRoot
    {
        public ulong Address { get; init; }
        public ulong Object { get; init; }
        public string? RootKind { get; init; }
        public string? ICorDebugType { get; init; }
        public ulong ExtraData { get; init; }
    }
}
