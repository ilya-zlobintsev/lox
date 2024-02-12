namespace Sharplox;

// The Statement visitor always returns null, because statements don't have a return value
public class Interpreter : IExpressionVisitor<object?>, IStatementVisitor<object?>
{
    Environment _environment = new();

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
    public object? VisitVariableExpression(VariableExpression expr) => _environment.Get(expr.Name);

    public object? VisitAssignmentExpression(AssignmentExpression expr)
    {
        var value = Evaluate(expr.Value);
        _environment.Assign(expr.Name, value);
        return value;
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

    // Utilities
    object? Evaluate(Expression expr) => expr.Accept(this);
    void Execute(Statement stmt) => stmt.Accept(this);

    void ExecuteBlock(List<Statement> statements, Environment currentEnvironment)
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

    bool IsTruthy(object data) => data switch
    {
        bool value => value,
        null => false,
        _ => true,
    };

    bool IsEqual(object? left, object? right) => left?.Equals(right) ?? right is null;

    void CheckNumberOperands(Token op, object? left, object? right)
    {
        if (left is not double)
            throw new RuntimeError(op, "Left operand is not a number");

        if (right is not double)
            throw new RuntimeError(op, "Right operand is not a number");
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