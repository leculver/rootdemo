// See https://aka.ms/new-console-template for more information

// usage:  HeapEnumerationTests.exe c:\path\to\dump_file c:\path\to\dbgshim.dll
// if you need dbgshim.dll, grab it from the nuget package, eg:
//          https://www.nuget.org/packages/Microsoft.Diagnostics.DbgShim.win-x64

using HeapEnumerationTests;
using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.DacInterface;
using System.Text.Json;

bool disableInteriorPointers = bool.Parse(args[2]);

using DataTarget dt = DataTarget.LoadDump(args[0]);
using ClrRuntime runtime = dt.ClrVersions.Single().CreateRuntime();

ICLRDebugging dbg = ICLRDebugging.Create(args[1]) ?? throw new Exception();
ICorDebugProcess5 process5 = new(dbg.CreateICorDebugProcess(runtime.ClrInfo, ICorDebugProcess5.IID_ICorDebugProcess5));
ICorDebugGCReferenceEnum refEnum = process5.EnumerateGCReferences(false) ?? throw new Exception();
if (disableInteriorPointers)
{
    using ICorDebugGCReferenceEnum2 refEnum2 = refEnum.GCReferenceEnum2 ?? throw new Exception();
    int hr = refEnum2.DisableInteriorPointerDecoding(true);
    if (hr == 0)
    {
        Console.WriteLine("Disabled interior pointer decoding.");
    }
    else
    {
        Console.WriteLine($"ICorDebugGCReferenceEnum2::DisableInteriorPointerDecoding failed: {(uint)hr:x}");
    }
}

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
            CorGCReferenceType.CorReferenceStackInterior => "StackInterior",
            CorGCReferenceType.CorHandleStrongAsyncPinned => "AsyncPinnedHandle",
            CorGCReferenceType.CorHandleStrong => "ClrRootKind.StrongHandle",
            CorGCReferenceType.CorHandleWeakRefCount or CorGCReferenceType.CorHandleStrongRefCount => "ClrRootKind.RefCountedHandle",
            CorGCReferenceType.CorHandleStrongDependent => "DependentHandle",
            _ => throw new Exception()
        },
        ExtraData = reference.ExtraData,
    });
}

WriteRoots(roots, ".icordebug.txt");
roots.Clear();

Console.WriteLine("ClrMD:");

foreach (ClrRoot root in runtime.Heap.EnumerateRoots())
{
    Console.WriteLine($"{root.RootKind}\t{root.Address:x12} -> {root.Object:x12}");
    roots.Add(new FoundRoot()
    {
        Address = root.Address,
        Object = root.Object,
        RootKind = root.RootKind.ToString(),
        ExtraData = root.IsInterior ? 1u : 0u
    });
}

WriteRoots(roots, ".clrmd.txt");
roots.Clear();

Console.WriteLine("ISOSDac:");
using var sos = runtime.DacLibrary.SOSDacInterface;
foreach (ClrThread thread in runtime.Threads)
{
    using var enumerator = sos.EnumerateStackRefs(thread.OSThreadId);
    foreach (StackRefData root in enumerator!.ReadStackRefs())
    {
        string interior = "";
        if ((root.Flags & 1) == 1)
            interior = " - interior";


        ClrObject obj = runtime.Heap.GetObject(root.Object);
        if (!obj.IsValid)
            obj = runtime.Heap.FindPreviousObjectOnSegment(root.Object);


        Console.WriteLine($"Stack - {(ulong)root.Address:x12} -> {(ulong)root.Object:x12}{interior} - ({obj.Address:x} {obj.Type?.Name}");

        roots.Add(new FoundRoot()
        {
            Address = root.Address,
            Object = root.Object,
            RootKind = ClrRootKind.Stack.ToString(),
            ExtraData = root.Flags
        });
    }
}
WriteRoots(roots, ".sos_stack.txt");
roots.Clear();

void WriteRoots(List<FoundRoot> roots, string filename)
{
    File.WriteAllText(args[0] + filename, JsonSerializer.Serialize(roots, new JsonSerializerOptions() { WriteIndented = true }));
}