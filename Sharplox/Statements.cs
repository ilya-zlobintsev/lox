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

public interface IStatementVisitor<out TR>
{
    TR VisitExpressionStatement(ExpressionStatement stmt);
    TR VisitPrintStatement(PrintStatement stmt);
    TR VisitVariableStatement(VariableStatement stmt);
    TR VisitBlockStatement(BlockStatement stmt);
}
