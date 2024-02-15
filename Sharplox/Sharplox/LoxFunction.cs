namespace Sharplox;

public class LoxFunction(FunctionExpression declaration, Environment closure, bool isInitializer) : ILoxCallable
{
    public int Arity() => declaration.Params.Count;

    public object? Call(Interpreter interpreter, IReadOnlyList<object?> arguments)
    {
        Environment environment = new(closure);

        for (var i = 0; i < declaration.Params.Count; i++)
            environment.Define(declaration.Params[i].Lexeme, arguments[i]);

        try
        {
            interpreter.ExecuteBlock(declaration.Body, environment);
        }
        catch (Return returnValue)
        {
            if (isInitializer) return closure.GetAt(0, "this");
            return returnValue.Value;
        }

        return isInitializer ? closure.GetAt(0, "this") : null;
    }

    public LoxFunction Bind(LoxInstance instance)
    {
        Environment environment = new(closure);
        environment.Define("this", instance);
        return new(declaration, environment, isInitializer);
    }

    public override string ToString()
    {
        var args = string.Join(", ", declaration.Params.Select(param => param.Lexeme));
        return $"<fn {declaration.Name?.Lexeme}({args})>";
    }
}
