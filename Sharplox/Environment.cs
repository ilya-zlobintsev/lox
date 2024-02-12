namespace Sharplox;

public class Environment(Environment? enclosing = null)
{
    readonly Dictionary<string, object?> _values = new();

    public object? Get(Token name)
    {
        if (_values.TryGetValue(name.Lexeme, out var value))
            return value;

        if (enclosing is null)
            throw new RuntimeError(name, $"Undefined variable '{name.Lexeme}'");

        return enclosing.Get(name);
    }

    public void Define(string name, object? value) => _values[name] = value;

    public void Assign(Token name, object? value)
    {
        if (_values.ContainsKey(name.Lexeme))
        {
            _values[name.Lexeme] = value;
            return;
        }

        if (enclosing is null)
            throw new RuntimeError(name, $"Undefined variable '{name.Lexeme}'");

        enclosing.Assign(name, value);
    }
}
