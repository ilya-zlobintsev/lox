namespace Sharplox;

public class LoxInstance(LoxClass klass)
{
    readonly Dictionary<string, object?> fields = new();

    public object? Get(Token name)
    {
        if (fields.TryGetValue(name.Lexeme, out var value))
            return value;

        if (klass.FindMethod(name.Lexeme) is { } method)
            return method.Bind(this);

        throw new RuntimeError(name, $"Undefined property '{name.Lexeme}'");
    }

    public void Set(Token name, object? value) => fields[name.Lexeme] = value;

    public override string ToString() => $"<instance of {klass.Name}>";
}
