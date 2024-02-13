using System.ComponentModel;
using System.Text;

namespace Sharplox;

public class AstPrinter : IExpressionVisitor<string>
{
    public static string Print(Expression expression) => expression.Accept(new AstPrinter());

    string PrintExpression(string name, params Expression[] expressions)
    {
        StringBuilder builder = new();

        builder.Append('(').Append(name);
        foreach (var expr in expressions)
            builder.Append(' ').Append(expr.Accept(this));

        builder.Append(')');

        return builder.ToString();
    }

    public string VisitUnaryExpression(UnaryExpression expr) => PrintExpression(expr.Operator.Lexeme, expr.Right);
    public string VisitBinaryExpression(BinaryExpression expr) => PrintExpression(expr.Operator.Lexeme, expr.Left, expr.Right);
    public string VisitGroupingExpression(GroupingExpression expr) => PrintExpression("group", expr.Expression);
    public string VisitVariableExpression(VariableExpression expr) => PrintExpression(expr.Name.Lexeme);
    public string VisitAssignmentExpression(AssignmentExpression expr) => PrintExpression("assign", expr);
    public string VisitLiteralExpression(LiteralExpression expr) => expr.Value?.ToString() ?? "nil";
    public string VisitLogicalExpression(LogicalExpression expr) => PrintExpression(expr.Operator.Lexeme, expr.Left, expr.Right);
}
