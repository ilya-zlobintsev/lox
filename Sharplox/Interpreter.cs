namespace Sharplox;

// The Statement visitor always returns null, because statements don't have a return value
public class Interpreter : IExpressionVisitor<object?>, IStatementVisitor<object?>
{
    readonly Environment _globals = new();
    Environment _environment;
    readonly Dictionary<Expression, int> _locals = new(ReferenceEqualityComparer.Instance);

    public Interpreter()
    {
        _environment = _globals;

        _globals.Define("clock", new Clock());
    }

    public object? Interpret(List<Statement> statements)
    {
        object? output = null;
        try
        {
            for (var i = 0; i < statements.Count; i++)
            {
                var statement = statements[i];
                if ((i == statements.Count - 1) && (statement is ExpressionStatement stmt))
                {
                    output = Evaluate(stmt.Expression);
                }
                else
                {
                    Execute(statement);
                }
            }
        }
        catch (RuntimeError error)
        {
            Lox.RuntimeError(error);
        }

        return output;
    }

    // Expressions
    public object? VisitUnaryExpression(UnaryExpression expr)
    {
        var right = Evaluate(expr.Right);
        if (right is null)
            return right;

        return expr.Operator.Type switch
        {
            TokenType.Bang => !IsTruthy(right),
            TokenType.Minus => -(double)right,
            _ => null,
        };
    }

    public object? VisitBinaryExpression(BinaryExpression expr)
    {
        var left = Evaluate(expr.Left);
        var right = Evaluate(expr.Right);

        switch (expr.Operator.Type)
        {
            case TokenType.Minus:
                CheckNumberOperands(expr.Operator, left, right);
                return (double?)left - (double?)right;
            case TokenType.Slash:
                CheckNumberOperands(expr.Operator, left, right);
                return (double?)left / (double?)right;
            case TokenType.Star:
                CheckNumberOperands(expr.Operator, left, right);
                return (double?)left * (double?)right;
            case TokenType.Plus:
                return Add(left, right);
            case TokenType.Greater:
                CheckNumberOperands(expr.Operator, left, right);
                return (double?)left > (double?)right;
            case TokenType.GreaterEqual:
                CheckNumberOperands(expr.Operator, left, right);
                return (double?)left >= (double?)right;
            case TokenType.Less:
                CheckNumberOperands(expr.Operator, left, right);
                return (double?)left < (double?)right;
            case TokenType.LessEqual:
                CheckNumberOperands(expr.Operator, left, right);
                return (double?)left <= (double?)right;
            case TokenType.EqualEqual:
                return IsEqual(left, right);
            case TokenType.BangEqual:
                return !IsEqual(left, right);
            default:
                return null;
        }

        object? Add(object? a, object? b) => (a, b) switch
        {
            (double leftValue, double rightValue) => leftValue + rightValue,
            (string leftValue, string rightValue) => leftValue + rightValue,
            (string leftValue, double rightValue) => leftValue + rightValue,
            (double leftValue, string rightValue) => leftValue + rightValue,
            _ => throw new RuntimeError(expr.Operator, "Operands must be either numbers or strings"),
        };
    }

    public object? VisitGroupingExpression(GroupingExpression expr) => Evaluate(expr.Expression);
    public object? VisitVariableExpression(VariableExpression expr) => LookupVariable(expr.Name, expr);

    object? LookupVariable(Token name, Expression expr)
    {
        if (_locals.TryGetValue(expr, out var local))
            return _environment.GetAt(local, name.Lexeme);

        return _globals.Get(name);
    }

    public object? VisitAssignmentExpression(AssignmentExpression expr)
    {
        var value = Evaluate(expr.Value);

        if (_locals.TryGetValue(expr, out var distance))
            _environment.AssignAt(distance, expr.Name, value);
        else
            _globals.Assign(expr.Name, value);

        return value;
    }

    public object? VisitLogicalExpression(LogicalExpression expr)
    {
        var left = Evaluate(expr.Left);

        if (expr.Operator.Type == TokenType.Or)
            if (IsTruthy(left))
                return left;

        if (expr.Operator.Type == TokenType.And)
            if (!IsTruthy(left))
                return left;

        return Evaluate(expr.Right);
    }

    public object? VisitCallExpression(CallExpression expr)
    {
        var callee = Evaluate(expr.Callee);

        var arguments = expr.Arguments.Select(Evaluate).ToArray();

        if (callee is ILoxCallable callable)
        {
            if (arguments.Length != callable.Arity())
                throw new RuntimeError(expr.Paren, $"Expected {callable.Arity()} arguments, got {arguments.Length}");

            return callable.Call(this, arguments);
        }

        throw new RuntimeError(expr.Paren, $"Only call functions and classes are callable");
    }

    public object? VisitLiteralExpression(LiteralExpression expr) => expr.Value;

    // Statements
    public object? VisitExpressionStatement(ExpressionStatement stmt)
    {
        Evaluate(stmt.Expression);
        return null;
    }

    public object? VisitPrintStatement(PrintStatement stmt)
    {
        var value = Evaluate(stmt.Expression);
        Console.WriteLine($"[lox]: {Stringify(value)}");
        return null;
    }

    public object? VisitVariableStatement(VariableStatement stmt)
    {
        object? value = null;
        if (stmt.Initializer is not null)
            value = Evaluate(stmt.Initializer);

        _environment.Define(stmt.Name.Lexeme, value);
        return null;
    }

    public object? VisitBlockStatement(BlockStatement stmt)
    {
        ExecuteBlock(stmt.Statements, new(_environment));
        return null;
    }

    public object? VisitIfStatement(IfStatement stmt)
    {
        if (IsTruthy(Evaluate(stmt.Condition)))
            Execute(stmt.ThenBranch);
        else if (stmt.ElseBranch is not null)
            Execute(stmt.ElseBranch);

        return null;
    }

    public object? VisitWhileStatement(WhileStatement stmt)
    {
        while (IsTruthy(Evaluate(stmt.Condition)))
        {
            Execute(stmt.Body);
        }

        return null;
    }

    public object VisitFunctionExpression(FunctionExpression expr)
    {
        LoxFunction function = new(expr, _environment, false);
        if (expr.Name is not null)
            _environment.Define(expr.Name.Lexeme, function);

        return function;
    }

    public object? VisitGetExpression(GetExpression expr)
    {
        var value = Evaluate(expr.Instance);
        if (value is LoxInstance instance)
            return instance.Get(expr.Name);

        throw new RuntimeError(expr.Name, $"Only instances have properties, got {value?.GetType().Name}");
    }

    public object? VisitSetExpression(SetExpression expr)
    {
        var instance = Evaluate(expr.Instance) as LoxInstance ?? throw new RuntimeError(expr.Name, "Only instances have fields");
        var value = Evaluate(expr.Value);
        instance.Set(expr.Name, value);

        return value;
    }

    public object? VisitThisExpression(ThisExpression expr) => LookupVariable(expr.Keyword, expr);

    public object? VisitSuperExpression(SuperExpression expr)
    {
        var distance = _locals[expr];
        if (distance == 0)
            throw new("Super expressions should always be in a scope, this is a bug");

        var superClass = (LoxClass)_environment.GetAt(distance, "super")!;
        var instance = (LoxInstance)_environment.GetAt(distance - 1, "this")!;

        var method = superClass.FindMethod(expr.Method.Lexeme) ??
                     throw new RuntimeError(expr.Method, $"Undefined property '{expr.Method.Lexeme}'");

        return method.Bind(instance);
    }

    public object VisitReturnStatement(ReturnStatement stmt)
    {
        object? returnValue = null;
        if (stmt.Value is not null)
            returnValue = Evaluate(stmt.Value);

        throw new Return(returnValue);
    }

    public object? VisitClassStatement(ClassStatement stmt)
    {
        LoxClass? superClass = null;
        if (stmt.Superclass is not null)
        {
            superClass = Evaluate(stmt.Superclass) as LoxClass ??
                         throw new RuntimeError(stmt.Superclass.Name, "Classes can only inherit from other classes");
        }

        _environment.Define(stmt.Name.Lexeme, null);

        if (stmt.Superclass is not null)
        {
            _environment = new(_environment);
            _environment.Define("super", superClass);
        }

        Dictionary<string, LoxFunction> methods = new();
        foreach (var method in stmt.Methods)
        {
            LoxFunction function = new(method, _environment, method.Name?.Lexeme == "init");
            if (method.Name is not null)
                methods.Add(method.Name.Lexeme, function);
        }

        LoxClass klass = new(stmt.Name.Lexeme, superClass, methods);

        if (superClass is not null)
            _environment = _environment.Enclosing!;

        _environment.Assign(stmt.Name, klass);
        return null;
    }

    // Utilities
    object? Evaluate(Expression expr) => expr.Accept(this);
    void Execute(Statement stmt) => stmt.Accept(this);

    public void ExecuteBlock(List<Statement> statements, Environment currentEnvironment)
    {
        var previousEnvironment = _environment;
        try
        {
            _environment = currentEnvironment;

            foreach (var statement in statements)
                Execute(statement);
        }
        finally
        {
            _environment = previousEnvironment;
        }
    }

    public void Resolve(Expression expr, int depth) => _locals[expr] = depth;

    bool IsTruthy(object? data) => data switch
    {
        bool value => value,
        null => false,
        _ => true,
    };

    bool IsEqual(object? left, object? right) => left?.Equals(right) ?? right is null;

    void CheckNumberOperands(Token op, object? left, object? right)
    {
        if (left is not double)
            throw new RuntimeError(op, $"Left operand is not a number (got type {left?.GetType().FullName})");

        if (right is not double)
            throw new RuntimeError(op, $"Right operand is not a number (got type {right?.GetType().FullName})");
    }

    public static string Stringify(object? value) => value switch
    {
        null => "nil",
        double number => number.ToString("G29"),
        _ => value.ToString()!,
    };
}

public class RuntimeError(Token token, string message) : Exception(message)
{
    public Token Token { get; } = token;
}

public class Return(object? value) : Exception(null)
{
    public object? Value { get; } = value;
}
