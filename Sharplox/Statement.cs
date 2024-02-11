namespace Sharplox;

public abstract record Statement
{
    public abstract TR Accept<TR>(IStatementVisitor<TR> visitor);
}

public record DeclarationStatement(Token Name, Expr? Initializer): Statement
{
    public override TR Accept<TR>(IStatementVisitor<TR> visitor) => visitor.VisitDeclarationStatement(this);
}

public record ExpressionStatement(Expr Expression) : Statement
{
    public override TR Accept<TR>(IStatementVisitor<TR> visitor) => visitor.VisitExpressionStatement(this);
}

public record PrintStatement(Expr Expression) : Statement
{
    public override TR Accept<TR>(IStatementVisitor<TR> visitor) => visitor.VisitPrintStatement(this);
}

public interface IStatementVisitor<TR>
{
    TR VisitExpressionStatement(ExpressionStatement stmt);
    TR VisitPrintStatement(PrintStatement stmt);
    TR VisitDeclarationStatement(DeclarationStatement stmt);
}