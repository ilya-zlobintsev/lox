namespace Sharplox;

public abstract record Expr
{
    public abstract TR Accept<TR>(IExpressionVisitor<TR> visitor);
}

public record UnaryExpr(Token Operator, Expr Right) : Expr
{
    public override TR Accept<TR>(IExpressionVisitor<TR> visitor) => visitor.VisitUnaryExpr(this);
}

public record BinaryExpr(Expr Left, Token Operator, Expr Right) : Expr
{
    public override TR Accept<TR>(IExpressionVisitor<TR> visitor) => visitor.VisitBinaryExpr(this);
}

public record GroupingExpr(Expr Expression) : Expr
{
    public override TR Accept<TR>(IExpressionVisitor<TR> visitor) => visitor.VisitGroupingExpr(this);
}

public record LiteralExpr(object? Value) : Expr
{
    public override TR Accept<TR>(IExpressionVisitor<TR> visitor) => visitor.VisitLiteralExpr(this);
}

public record VariableExpr(Token Name) : Expr
{
    public override TR Accept<TR>(IExpressionVisitor<TR> visitor) => visitor.VisitVariableExpr(this);
}

public interface IExpressionVisitor<TR>
{
    TR VisitUnaryExpr(UnaryExpr expr);
    TR VisitBinaryExpr(BinaryExpr expr);
    TR VisitGroupingExpr(GroupingExpr expr);
    TR VisitLiteralExpr(LiteralExpr expr);
    TR VisitVariableExpr(VariableExpr expr);
}
