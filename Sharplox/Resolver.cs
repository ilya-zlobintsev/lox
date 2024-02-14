namespace Sharplox;

public class Resolver(Interpreter interpreter) : IExpressionVisitor<object?>, IStatementVisitor<object?>
{
    readonly List<Dictionary<string, bool>> _scopes = new();
    FunctionType _currentFunction = FunctionType.None;
    Dictionary<string, bool>? TopScope => _scopes.Count == 0 ? null : _scopes[^1];

    enum FunctionType
    {
        None,
        Function,
    }

    public void Resolve(List<Statement> statements)
    {
        foreach (var statement in statements)
            Resolve(statement);
    }

    public object? VisitUnaryExpression(UnaryExpression expr)
    {
        Resolve(expr.Right);
        return null;
    }

    public object? VisitBinaryExpression(BinaryExpression expr)
    {
        Resolve(expr.Left);
        Resolve(expr.Right);
        return null;
    }

    public object? VisitGroupingExpression(GroupingExpression expr)
    {
        Resolve(expr.Expression);
        return null;
    }

    public object? VisitLiteralExpression(LiteralExpression expr) => null;

    public object? VisitVariableExpression(VariableExpression expr)
    {
        var scope = TopScope;
        if (scope is not null && scope.Count != 0 && scope.ContainsKey(expr.Name.Lexeme) && scope[expr.Name.Lexeme] == false)
            Lox.Error(expr.Name, "Variable cannot be accessed from its own initializer");

        ResolveLocal(expr, expr.Name);
        return null;
    }

    public object? VisitAssignmentExpression(AssignmentExpression expr)
    {
        Resolve(expr.Value);
        ResolveLocal(expr, expr.Name);
        return null;
    }

    public object? VisitLogicalExpression(LogicalExpression expr)
    {
        Resolve(expr.Left);
        Resolve(expr.Right);
        return null;
    }

    public object? VisitCallExpression(CallExpression expr)
    {
        Resolve(expr.Callee);

        foreach (var argument in expr.Arguments)
            Resolve(argument);

        return null;
    }

    public object? VisitFunctionExpression(FunctionExpression expr)
    {
        if (expr.Name is not null)
        {
            Declare(expr.Name);
            Define(expr.Name);
        }

        ResolveFunction(expr, FunctionType.Function);
        return null;
    }

    public object? VisitExpressionStatement(ExpressionStatement stmt)
    {
        Resolve(stmt.Expression);
        return null;
    }

    public object? VisitPrintStatement(PrintStatement stmt)
    {
        Resolve(stmt.Expression);
        return null;
    }

    public object? VisitVariableStatement(VariableStatement stmt)
    {
        Declare(stmt.Name);

        if (stmt.Initializer is not null)
            Resolve(stmt.Initializer);

        Define(stmt.Name);
        return null;
    }

    public object? VisitBlockStatement(BlockStatement stmt)
    {
        BeginScope();
        Resolve(stmt.Statements);
        EndScope();
        return null;
    }

    public object? VisitIfStatement(IfStatement stmt)
    {
        Resolve(stmt.Condition);
        Resolve(stmt.ThenBranch);
        if (stmt.ElseBranch is not null)
            Resolve(stmt.ElseBranch);

        return null;
    }

    public object? VisitWhileStatement(WhileStatement stmt)
    {
        Resolve(stmt.Condition);
        Resolve(stmt.Body);
        return null;
    }

    public object? VisitReturnStatement(ReturnStatement stmt)
    {
        if (stmt.Value is not null)
            Resolve(stmt.Value);

        return null;
    }

    void Resolve(Statement statement) => statement.Accept(this);
    void Resolve(Expression expression) => expression.Accept(this);

    void BeginScope() => _scopes.Add(new());
    void EndScope() => _scopes.RemoveAt(_scopes.Count - 1);

    void Declare(Token name)
    {
        var scope = TopScope;
        if (scope is null) return;

        if (scope.ContainsKey(name.Lexeme))
            Lox.Error(name, $"Variable {name.Lexeme} already exists in the current scope");

        scope.Add(name.Lexeme, false);
    }

    void Define(Token name)
    {
        var scope = TopScope;
        if (scope is null) return;
        scope[name.Lexeme] = true;
    }

    void ResolveLocal(Expression expr, Token name)
    {
        for (var i = _scopes.Count - 1; i >= 0; i--)
            if (_scopes[i].ContainsKey(name.Lexeme))
            {
                interpreter.Resolve(expr, _scopes.Count - 1 - i);
                return;
            }
    }

    void ResolveFunction(FunctionExpression function, FunctionType type)
    {
        var enclosingFunction = _currentFunction;
        _currentFunction = type;

        BeginScope();
        foreach (var param in function.Params)
        {
            Declare(param);
            Define(param);
        }

        Resolve(function.Body);
        EndScope();

        _currentFunction = enclosingFunction;
    }
}
