namespace Sharplox.Tests;

public class AstPrinting
{
    [Fact]
    public void PrintBasicExpression()
    {
        var expression = new BinaryExpression(
            new UnaryExpression(
                new(TokenType.Minus, "-", null, 1),
                new LiteralExpression(123)
            ),
            new(TokenType.Star, "*", null, 1),
            new GroupingExpression(
                new LiteralExpression(45.67)
            )
        );
        Assert.Equal("(* (- 123) (group 45.67))", AstPrinter.Print(expression));
    }
}
