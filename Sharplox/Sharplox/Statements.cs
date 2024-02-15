namespace Sharplox;

public abstract record Statement
{
    public abstract TR Accept<TR>(IStatementVisitor<TR> visitor);
}

public record VariableStatement(Token Name, Expression? Initializer) : Statement
{
    public override TR Accept<TR>(IStatementVisitor<TR> visitor) => visitor.VisitVariableStatement(this);
}

public record ExpressionStatement(Expression Expression) : Statement
{
    public override TR Accept<TR>(IStatementVisitor<TR> visitor) => visitor.VisitExpressionStatement(this);
}

public record BlockStatement(List<Statement> Statements) : Statement
{
    public override TR Accept<TR>(IStatementVisitor<TR> visitor) => visitor.VisitBlockStatement(this);
}

public record PrintStatement(Expression Expression) : Statement
{
    public override TR Accept<TR>(IStatementVisitor<TR> visitor) => visitor.VisitPrintStatement(this);
}

public record IfStatement(Expression Condition, Statement ThenBranch, Statement? ElseBranch) : Statement
{
    public override TR Accept<TR>(IStatementVisitor<TR> visitor) => visitor.VisitIfStatement(this);
}

public record LoopStatement(Expression Condition, Statement Body, Expression? Increment) : Statement
{
    public override TR Accept<TR>(IStatementVisitor<TR> visitor) => visitor.VisitLoopStatement(this);
}

public record ReturnStatement(Token Keyword, Expression? Value) : Statement
{
    public override TR Accept<TR>(IStatementVisitor<TR> visitor) => visitor.VisitReturnStatement(this);
}

public record BreakStatement(Token Keyword) : Statement
{
    public override TR Accept<TR>(IStatementVisitor<TR> visitor) => visitor.VisitBreakStatement(this);
}

public record ContinueStatement(Token Keyword) : Statement
{
    public override TR Accept<TR>(IStatementVisitor<TR> visitor) => visitor.VisitContinueStatement(this);
}

public record ClassStatement(Token Name, VariableExpression? Superclass, List<FunctionExpression> Methods) : Statement
{
    public override TR Accept<TR>(IStatementVisitor<TR> visitor) => visitor.VisitClassStatement(this);
}

public interface IStatementVisitor<out TR>
{
    TR VisitExpressionStatement(ExpressionStatement stmt);
    TR VisitPrintStatement(PrintStatement stmt);
    TR VisitVariableStatement(VariableStatement stmt);
    TR VisitBlockStatement(BlockStatement stmt);
    TR VisitIfStatement(IfStatement stmt);
    TR VisitLoopStatement(LoopStatement stmt);
    TR VisitReturnStatement(ReturnStatement stmt);
    TR VisitClassStatement(ClassStatement stmt);
    TR VisitBreakStatement(BreakStatement stmt);
    TR VisitContinueStatement(ContinueStatement stmt);
}
