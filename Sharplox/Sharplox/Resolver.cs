namespace Sharplox;

public class Resolver(Interpreter interpreter) : IExpressionVisitor<object?>, IStatementVisitor<object?>
{
    readonly List<Dictionary<string, bool>> _scopes = [];
    FunctionType _currentFunction = FunctionType.None;
    ClassType _currentClass = ClassType.None;
    LoopType _currentLoop = LoopType.None;
    Dictionary<string, bool>? TopScope => _scopes.Count == 0 ? null : _scopes[^1];

    enum FunctionType
    {
        None,
        Function,
        Initializer,
        Method,
    }

    enum ClassType
    {
        None,
        Class,
        Subclass,
    }

    enum LoopType
    {
        None,
        Loop,
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

    public object? VisitGetExpression(GetExpression expr)
    {
        Resolve(expr.Instance);
        return null;
    }

    public object? VisitSetExpression(SetExpression expr)
    {
        Resolve(expr.Value);
        Resolve(expr.Instance);
        return null;
    }

    public object? VisitThisExpression(ThisExpression expr)
    {
        if (_currentClass == ClassType.None)
        {
            Lox.Error(expr.Keyword, "Cannot use 'this' outside of a class");
            return null;
        }

        ResolveLocal(expr, expr.Keyword);
        return null;
    }

    public object? VisitSuperExpression(SuperExpression expr)
    {
        switch (_currentClass)
        {
            case ClassType.None:
                Lox.Error(expr.Keyword, "Cannot use 'super' outside of a class");
                break;
            case ClassType.Class:
                Lox.Error(expr.Keyword, "Cannot use 'super' in a class with no subclass");
                break;
            default:
                ResolveLocal(expr, expr.Keyword);
                break;
        }

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

    public object? VisitLoopStatement(LoopStatement stmt)
    {
        Resolve(stmt.Condition);
        if (stmt.Increment is not null)
            Resolve(stmt.Increment);

        var surroundingType = _currentLoop;
        _currentLoop = LoopType.Loop;
        Resolve(stmt.Body);
        _currentLoop = surroundingType;

        return null;
    }

    public object? VisitReturnStatement(ReturnStatement stmt)
    {
        if (stmt.Value is null) return null;

        if (_currentFunction == FunctionType.Initializer)
            Lox.Error(stmt.Keyword, "'return' is not allowed in an initializer");

        Resolve(stmt.Value);

        return null;
    }

    public object? VisitClassStatement(ClassStatement stmt)
    {
        var enclosingClass = _currentClass;
        _currentClass = ClassType.Class;

        Declare(stmt.Name);
        Define(stmt.Name);

        if (stmt.Superclass is not null)
        {
            if (stmt.Superclass.Name.Lexeme == stmt.Name.Lexeme)
                Lox.Error(stmt.Superclass.Name, "Class cannot inherit from itself");

            _currentClass = ClassType.Subclass;
            Resolve(stmt.Superclass);
        }

        if (stmt.Superclass is not null)
        {
            BeginScope();
            TopScope!.Add("super", true);
        }

        BeginScope();
        TopScope?.Add("this", true);

        foreach (var method in stmt.Methods)
        {
            var declaration = method.Name?.Lexeme == "init" ? FunctionType.Initializer : FunctionType.Method;
            ResolveFunction(method, declaration);
        }

        EndScope();
        if (stmt.Superclass is not null) EndScope();

        _currentClass = enclosingClass;
        return null;
    }

    public object? VisitBreakStatement(BreakStatement stmt)
    {
        if (_currentLoop == LoopType.None)
            Lox.Error(stmt.Keyword, "'break' can only be used in loops");

        return null;
    }

    public object? VisitContinueStatement(ContinueStatement stmt)
    {
        if (_currentLoop == LoopType.None)
            Lox.Error(stmt.Keyword, "'continue' can only be used in loops");

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
