public abstract record RuntimeValues;

public sealed record IntValue(int Value) : RuntimeValues;
public sealed record BoolValue(bool Value) : RuntimeValues;
public sealed record PidValue(int Value) : RuntimeValues;

public sealed class RuntimeException : Exception
{
    public RuntimeException(string message) : base(message){}
}

