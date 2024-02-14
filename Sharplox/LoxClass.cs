namespace Sharplox;

public class LoxClass(string name, LoxClass? superclass, Dictionary<string, LoxFunction> methods) : ILoxCallable
{
    public string Name => name;

    public object? Call(Interpreter interpreter, IReadOnlyList<object?> arguments)
    {
        LoxInstance instance = new(this);
        var initializer = FindMethod("init");
        initializer?.Bind(instance).Call(interpreter, arguments);

        return instance;
    }

    public LoxFunction? FindMethod(string methodName) =>
        methods.GetValueOrDefault(methodName) ?? superclass?.FindMethod(methodName);

    public int Arity() => FindMethod("init")?.Arity() ?? 0;

    public override string ToString() => $"<class {Name}>";
}
