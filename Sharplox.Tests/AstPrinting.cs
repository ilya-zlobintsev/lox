namespace Sharplox.Tests;

public class AstPrinting
{
    [Fact]
    public void PrintBasicExpression()
    {
        var expression = new BinaryExpr(
            new UnaryExpr(
                new(TokenType.Minus, "-", null, 1),
                new LiteralExpr(123)
            ),
            new(TokenType.Star, "*", null, 1),
            new GroupingExpr(
                new LiteralExpr(45.67)
            )
        );
        Assert.Equal("(* (- 123) (group 45.67))", AstPrinter.Print(expression));
    }
}
