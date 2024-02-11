using System.ComponentModel;
using System.Text;

namespace Sharplox;

public class AstPrinter : IExpressionVisitor<string>
{
    public static string Print(Expr expr) => expr.Accept(new AstPrinter());

    string PrintExpression(string name, params Expr[] expressions)
    {
        StringBuilder builder = new();

        builder.Append('(').Append(name);
        foreach (var expr in expressions)
            builder.Append(' ').Append(expr.Accept(this));

        builder.Append(')');

        return builder.ToString();
    }

    public string VisitUnaryExpr(UnaryExpr expr) => PrintExpression(expr.Operator.Lexeme, expr.Right);
    public string VisitBinaryExpr(BinaryExpr expr) => PrintExpression(expr.Operator.Lexeme, expr.Left, expr.Right);
    public string VisitGroupingExpr(GroupingExpr expr) => PrintExpression("group", expr.Expression);
    public string VisitVariableExpr(VariableExpr expr) => PrintExpression(expr.Name.Lexeme);
    public string VisitLiteralExpr(LiteralExpr expr) => expr.Value?.ToString() ?? "nil";
}
