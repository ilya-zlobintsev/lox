namespace Sharplox;

public class Environment(Environment? enclosing = null)
{
    readonly Dictionary<string, object?> _values = new();
    private readonly Environment? _enclosing = enclosing;

    public object? Get(Token name)
    {
        if (_values.TryGetValue(name.Lexeme, out var value))
            return value;

        if (_enclosing is null)
            throw new RuntimeError(name, $"Undefined variable '{name.Lexeme}'");

        return _enclosing.Get(name);
    }

    public void Define(string name, object? value) => _values[name] = value;

    public void Assign(Token name, object? value)
    {
        if (_values.ContainsKey(name.Lexeme))
        {
            _values[name.Lexeme] = value;
            return;
        }

        if (_enclosing is null)
            throw new RuntimeError(name, $"Undefined variable '{name.Lexeme}'");

        _enclosing.Assign(name, value);
    }

    public object? GetAt(int distance, string name) => Ancestor(distance)._values.GetValueOrDefault(name);
    public object? AssignAt(int distance, Token name, object? value) => Ancestor(distance)._values[name.Lexeme] = value;

    Environment Ancestor(int distance)
    {
        var environment = this;
        for (var i = 0; i < distance; i++)
            environment = environment!._enclosing;

        return environment!;
    }
}
