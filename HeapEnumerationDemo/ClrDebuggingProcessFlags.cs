namespace HeapEnumerationTests
{
    public enum ClrDebuggingProcessFlags
    {
        // This CLR has a non-catchup managed debug event to send after jit attach is complete
        ManagedDebugEventPending = 1,
        ManagedDebugEventDebuggerLaunch = 2
    }
}
