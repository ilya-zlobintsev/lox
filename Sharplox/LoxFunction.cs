namespace Sharplox;

public class LoxFunction(FunctionStatement declaration, Environment Closure) : ILoxCallable
{
    public int Arity() => declaration.Params.Count;

    public object? Call(Interpreter interpreter, IReadOnlyList<object?> arguments)
    {
        Environment environment = new(Closure);

        for (var i = 0; i < declaration.Params.Count; i++)
            environment.Define(declaration.Params[i].Lexeme, arguments[i]);

        try
        {
            interpreter.ExecuteBlock(declaration.Body, environment);
        }
        catch (Return returnValue)
        {
            return returnValue.Value;
        }

        return null;
    }

    public override string ToString()
    {
        var args = string.Join(", ", declaration.Params.Select(param => param.Lexeme));
        return $"<fn {declaration.Name.Lexeme}({args})>";
    }
}
