namespace Sharplox;

public class Parser(IReadOnlyList<Token> tokens)
{
    int _current;

    public List<Statement> Parse()
    {
        List<Statement> statements = new();
        while (!IsAtEnd())
            statements.Add(Statement());

        return statements;
    }

    Statement Statement()
    {
        if (CurrentMatches(TokenType.Print)) return PrintStatement();
        return ExpressionStatement();
    }

    Statement PrintStatement()
    {
        var value = Expression();
        Consume(TokenType.Semicolon, "Missing ';' after a value");
        return new PrintStatement(value);
    }

    Statement ExpressionStatement()
    {
        var value = Expression();
        Consume(TokenType.Semicolon, "Missing ';' after an expression");
        return new ExpressionStatement(value);
    }

    Expr Expression() => Equality();
    Expr Equality() => Binary(Comparison, TokenType.BangEqual, TokenType.EqualEqual);
    Expr Comparison() => Binary(Term, TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual);
    Expr Term() => Binary(Factor, TokenType.Minus, TokenType.Plus);
    Expr Factor() => Binary(Unary, TokenType.Slash, TokenType.Star);

    Expr Binary(Func<Expr> nextExpr, params TokenType[] tokenTypes)
    {
        var expr = nextExpr();

        while (CurrentMatches(tokenTypes))
        {
            var op = PreviousToken();
            var right = nextExpr();
            expr = new BinaryExpr(expr, op, right);
        }

        return expr;
    }

    Expr Unary()
    {
        if (CurrentMatches(TokenType.Bang, TokenType.Minus))
        {
            var op = PreviousToken();
            var right = Unary();
            return new UnaryExpr(op, right);
        }

        return Primary();
    }

    Expr Primary()
    {
        if (CurrentMatches(TokenType.False)) return new LiteralExpr(false);
        if (CurrentMatches(TokenType.True)) return new LiteralExpr(true);
        if (CurrentMatches(TokenType.Nil)) return new LiteralExpr(null);

        if (CurrentMatches(TokenType.Number, TokenType.String))
            return new LiteralExpr(PreviousToken().Literal!);

        if (CurrentMatches(TokenType.LeftParen))
        {
            var expr = Expression();
            Consume(TokenType.RightParen, "Expected a closing ')' after expression");
            return new GroupingExpr(expr);
        }

        throw Error(Peek(), "Not a valid expression start symbol");
    }

    bool CurrentMatches(params TokenType[] types)
    {
        var value = types.Any(CheckCurrent);
        if (value)
            Advance();

        return value;
    }

    Token Advance()
    {
        if (!IsAtEnd()) _current++;
        return PreviousToken();
    }

    Token Consume(TokenType type, string message) => CheckCurrent(type) ? Advance() : throw Error(Peek(), message);

    bool CheckCurrent(TokenType type) => !IsAtEnd() && Peek().Type == type;
    bool IsAtEnd() => Peek().Type == TokenType.Eof;
    Token Peek() => tokens[_current];
    Token PreviousToken() => tokens[_current - 1];

    ParseError Error(Token token, string message)
    {
        Lox.Error(token, message);
        return new();
    }

    void Synchronize()
    {
        Advance();

        while (!IsAtEnd())
        {
            if (PreviousToken().Type == TokenType.Semicolon) return;

            switch (Peek().Type)
            {
                case TokenType.Class:
                case TokenType.Fun:
                case TokenType.Var:
                case TokenType.For:
                case TokenType.If:
                case TokenType.While:
                case TokenType.Print:
                case TokenType.Return:
                    return;
            }

            Advance();
        }
    }

    private class ParseError : Exception;
}
