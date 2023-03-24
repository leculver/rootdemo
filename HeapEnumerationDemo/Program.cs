// See https://aka.ms/new-console-template for more information

// usage:  HeapEnumerationTests.exe c:\path\to\dump_file c:\path\to\dbgshim.dll
// if you need dbgshim.dll, grab it from the nuget package, eg:
//          https://www.nuget.org/packages/Microsoft.Diagnostics.DbgShim.win-x64

using HeapEnumerationTests;
using Microsoft.Diagnostics.Runtime;
using System.Text.Json;

using DataTarget dt = DataTarget.LoadDump(args[0]);
using ClrRuntime runtime = dt.ClrVersions.Single().CreateRuntime();

ICLRDebugging dbg = ICLRDebugging.Create(args[1]) ?? throw new Exception();
ICorDebugProcess5 process5 = new(dbg.CreateICorDebugProcess(runtime.ClrInfo, ICorDebugProcess5.IID_ICorDebugProcess5));
ICorDebugGCReferenceEnum refEnum = process5.EnumerateGCReferences(false) ?? throw new Exception();

List<FoundRoot> roots = new();
Console.WriteLine("ICorDebug:");
foreach (COR_GC_REFERENCE reference in refEnum.EnumerateReferences())
{
    if (reference.Type == 0)
        continue;
    Console.WriteLine(reference);
    using ICorDebugValue value = new(reference.Location);

    ulong addr = 0;
    ulong obj = 0;
    string icordebugtype;
    if (value.IsObjectValue)
    {
        icordebugtype = "ICorDebugObjectValue";
        obj = value.GetAddress();
    }
    else if (value.IsReferenceValue)
    {
        icordebugtype = "ICorDebugReferenceValue";
        addr = value.GetAddress();
    }
    else
    {
        throw new Exception();
    }


    roots.Add(new FoundRoot()
    {
        Address = addr,
        Object = obj,
        ICorDebugType = icordebugtype,
        RootKind = reference.Type switch
        {
            CorGCReferenceType.CorHandleStrongPinning => "PinnedHandle",
            CorGCReferenceType.CorReferenceStack => "Stack",
            CorGCReferenceType.CorHandleStrongAsyncPinned => "AsyncPinnedHandle",
            CorGCReferenceType.CorHandleStrong => "ClrRootKind.StrongHandle",
            CorGCReferenceType.CorHandleWeakRefCount or CorGCReferenceType.CorHandleStrongRefCount => "ClrRootKind.RefCountedHandle",
            CorGCReferenceType.CorHandleStrongDependent => "DependentHandle",
            _ => throw new Exception()
        },
        ExtraData = reference.ExtraData,
    });
}

File.WriteAllText("icordebug.txt", JsonSerializer.Serialize(roots, new JsonSerializerOptions() { WriteIndented = true }));
roots.Clear();

Console.WriteLine("ClrMD:");

foreach (IClrRoot root in runtime.Heap.EnumerateRoots())
{
    Console.WriteLine($"{root.RootKind}\t{root.Address:x12} -> {root.Object:x12}");
    roots.Add(new FoundRoot()
    {
        Address = root.Address,
        Object = root.Object,
        RootKind = root.RootKind.ToString(),
        ExtraData = root is ClrStackRoot stackRoot && stackRoot.IsInterior ? 1u : 0u
    });
}

File.WriteAllText("clrmd.txt", JsonSerializer.Serialize(roots, new JsonSerializerOptions() { WriteIndented = true }));
roots.Clear();
