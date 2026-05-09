public sealed record Message(int SenderPid, int Value);
public sealed class Mailbox
{
    private readonly Queue<Message> _messages = new();
    private readonly object _lock = new();

    public void Send(Message message)
    {
        lock (_lock)
        {
            _messages.Enqueue(message);
            Monitor.PulseAll(_lock);
        }
    }

    public Message ReceiveFrom(int expectedSenderPid)
    {
        lock (_lock)
        {
            while (true)
            {
                var count = _messages.Count;
                Message? matched = null;

                for (var i = 0; i < count; i++)
                {
                    var current = _messages.Dequeue();
                    if (matched is null && current.SenderPid == expectedSenderPid)
                    {
                        matched = current;
                        continue;
                    }

                    _messages.Enqueue(current);
                }

                if (matched is not null)
                {
                    return matched;
                }

                Monitor.Wait(_lock);
            }
        }
    }
}

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