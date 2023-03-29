using System.Diagnostics;
using System.Runtime.CompilerServices;

internal unsafe class Program
{
    private static void Main(string[] args)
    {
        BlockFQ();

        ConditionalWeakTable<object, object> condWeak = new();
        ConditionalSource source = new ConditionalSource();
        condWeak.Add(source, new ConditionalTarget());

        TestStruct[] structArray = new TestStruct[100];
        fixed (TestStruct* ptr = &structArray[50])
        {
            TestWithPointer(ptr);
        }

        GC.KeepAlive(structArray);
        GC.KeepAlive(condWeak);
        GC.KeepAlive(source);
    }

    private static void BlockFQ()
    {
        _ = new FinalizerBlocker();
        _ = new FinalizerBlocker();
        _ = new FinalizerBlocker();
        _ = new FinalizerBlocker();
        _ = new FinalizerBlocker();
    }

    private static void TestWithPointer(TestStruct* ptr)
    {
        TestWithReference(ref *ptr);
    }

    private static void TestWithReference(ref TestStruct st)
    {
        Console.WriteLine(st.b);
        GC.Collect();
        while(!FinalizerBlocker.evt.Wait(250))
            GC.Collect();

        Debugger.Break();
    }
}

class FinalizerBlocker
{
    public static ManualResetEventSlim evt = new ManualResetEventSlim(false);

    ~FinalizerBlocker()
    {
        Console.WriteLine("FQ blocked with roots");
        evt.Set();
        while(true)
        {
            Thread.Sleep(10_000);
        }
    }
}

struct TestStruct
{
    public int a, b, c;
}

class ConditionalSource { }
class ConditionalTarget { }

