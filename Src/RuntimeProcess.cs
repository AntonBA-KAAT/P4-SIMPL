using System.Collections.Concurrent;

public sealed record Message(int SenderPid, int Value);

public sealed class RuntimeProcess
{
    public int Pid {get;}
    public Dictionary<string, RuntimeValues> Store {get;}

    public RuntimeProcess(int pid, Dictionary<string, RuntimeValues>? store = null)
    {
        Pid = pid;
        Store = store ?? new Dictionary<string, RuntimeValues>();
    }
}