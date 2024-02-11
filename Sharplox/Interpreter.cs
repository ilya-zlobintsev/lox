namespace Sharplox;

public class Interpreter : IExpressionVisitor<object?>, IStatementVisitor<object?>
{
    public void Interpret(IReadOnlyList<Statement> statements)
    {
        try
        {
            foreach (var statement in statements)
                Execute(statement);
        }
        catch (RuntimeError error)
        {
            Lox.RuntimeError(error);
        }
    }
    
    // Expressions
    public object? VisitUnaryExpr(UnaryExpr expr)
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

    public object? VisitBinaryExpr(BinaryExpr expr)
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

    public object? VisitGroupingExpr(GroupingExpr expr) => Evaluate(expr.Expression);
    public object? VisitVariableExpr(VariableExpr expr) => throw new NotImplementedException();
    public object? VisitLiteralExpr(LiteralExpr expr) => expr.Value;

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

    public object? VisitDeclarationStatement(DeclarationStatement stmt) => throw new NotImplementedException();

    // Utilities
    object? Evaluate(Expr expr) => expr.Accept(this);
    object? Execute(Statement stmt) => stmt.Accept(this);

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

    string Stringify(object? value) => value switch
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