namespace Sharplox;

public abstract record Expression
{
    public abstract TR Accept<TR>(IExpressionVisitor<TR> visitor);
}

public record UnaryExpression(Token Operator, Expression Right) : Expression
{
    public override TR Accept<TR>(IExpressionVisitor<TR> visitor) => visitor.VisitUnaryExpression(this);
}

public record BinaryExpression(Expression Left, Token Operator, Expression Right) : Expression
{
    public override TR Accept<TR>(IExpressionVisitor<TR> visitor) => visitor.VisitBinaryExpression(this);
}

public record LogicalExpression(Expression Left, Token Operator, Expression Right) : Expression
{
    public override TR Accept<TR>(IExpressionVisitor<TR> visitor) => visitor.VisitLogicalExpression(this);
}

public record GroupingExpression(Expression Expression) : Expression
{
    public override TR Accept<TR>(IExpressionVisitor<TR> visitor) => visitor.VisitGroupingExpression(this);
}

public record LiteralExpression(object? Value) : Expression
{
    public override TR Accept<TR>(IExpressionVisitor<TR> visitor) => visitor.VisitLiteralExpression(this);
}

public record VariableExpression(Token Name) : Expression
{
    public override TR Accept<TR>(IExpressionVisitor<TR> visitor) => visitor.VisitVariableExpression(this);
}

public record AssignmentExpression(Token Name, Expression Value) : Expression
{
    public override TR Accept<TR>(IExpressionVisitor<TR> visitor) => visitor.VisitAssignmentExpression(this);
}

public record CallExpression(Expression Callee, Token Paren, List<Expression> Arguments) : Expression
{
    public override TR Accept<TR>(IExpressionVisitor<TR> visitor) => visitor.VisitCallExpression(this);
}

public interface IExpressionVisitor<out TR>
{
    TR VisitUnaryExpression(UnaryExpression expr);
    TR VisitBinaryExpression(BinaryExpression expr);
    TR VisitGroupingExpression(GroupingExpression expr);
    TR VisitLiteralExpression(LiteralExpression expr);
    TR VisitVariableExpression(VariableExpression expr);
    TR VisitAssignmentExpression(AssignmentExpression expr);
    TR VisitLogicalExpression(LogicalExpression expr);
    TR VisitCallExpression(CallExpression expr);
}
