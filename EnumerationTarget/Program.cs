using System.Diagnostics;

internal unsafe class Program
{
    private static void Main(string[] args)
    {
        BlockFQ();

        TestStruct[] structArray = new TestStruct[100];
        fixed (TestStruct* ptr = &structArray[50])
        {
            TestWithPointer(ptr);
        }

        GC.KeepAlive(structArray);
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

