namespace Sharplox;

public interface ILoxCallable
{
    int Arity();
    object? Call(Interpreter interpreter, IReadOnlyList<object?> arguments);
}

public class Clock : ILoxCallable
{
    public int Arity() => 0;

    public object? Call(Interpreter interpreter, IReadOnlyList<object?> arguments) =>
        DateTime.Now.Subtract(DateTime.UnixEpoch).TotalMicroseconds / 1000.0 / 1000.0;
}
