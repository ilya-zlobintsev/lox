namespace Sharplox;

public struct Parser(IReadOnlyList<Token> tokens)
{
    int _current;

    public List<Statement> Parse()
    {
        List<Statement> statements = new();
        while (!IsAtEnd())
            if (Declaration() is { } statement)
                statements.Add(statement);

        return statements;
    }

    Statement? Declaration()
    {
        try
        {
            if (CurrentMatches(TokenType.Var)) return VariableDeclaration();
            return Statement();
        }
        catch (ParseError)
        {
            PanicAndSynchronize();
            return null;
        }
    }

    Statement VariableDeclaration()
    {
        var name = Consume(TokenType.Identifier, "Expected a valid variable name");
        Expression? initializer = null;
        if (CurrentMatches(TokenType.Equal))
            initializer = Expression();

        Consume(TokenType.Semicolon, "Expected a ';' after a variable declaration");
        return new VariableStatement(name, initializer);
    }

    Statement Statement()
    {
        if (CurrentMatches(TokenType.Print)) return PrintStatement();
        if (CurrentMatches(TokenType.LeftBrace)) return BlockStatement();
        return ExpressionStatement();
    }

    Statement BlockStatement() => new BlockStatement(Block());

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

    Expression Expression() => Assignment();

    Expression Assignment()
    {
        var expr = Equality();

        if (CurrentMatches(TokenType.Equal))
        {
            var equals = PreviousToken();
            var value = Assignment();

            if (expr is VariableExpression var)
                return new AssignmentExpression(var.Name, value);

            Error(equals, "Invalid assignment target");
        }

        return expr;
    }

    Expression Equality() => Binary(Comparison, TokenType.BangEqual, TokenType.EqualEqual);
    Expression Comparison() => Binary(Term, TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual);
    Expression Term() => Binary(Factor, TokenType.Minus, TokenType.Plus);
    Expression Factor() => Binary(Unary, TokenType.Slash, TokenType.Star);

    Expression Binary(Func<Expression> nextExpr, params TokenType[] tokenTypes)
    {
        var expr = nextExpr();

        while (CurrentMatches(tokenTypes))
        {
            var op = PreviousToken();
            var right = nextExpr();
            expr = new BinaryExpression(expr, op, right);
        }

        return expr;
    }

    Expression Unary()
    {
        if (CurrentMatches(TokenType.Bang, TokenType.Minus))
        {
            var op = PreviousToken();
            var right = Unary();
            return new UnaryExpression(op, right);
        }

        return Primary();
    }

    Expression Primary()
    {
        if (CurrentMatches(TokenType.False)) return new LiteralExpression(false);
        if (CurrentMatches(TokenType.True)) return new LiteralExpression(true);
        if (CurrentMatches(TokenType.Nil)) return new LiteralExpression(null);

        if (CurrentMatches(TokenType.Number, TokenType.String))
            return new LiteralExpression(PreviousToken().Literal!);

        if (CurrentMatches(TokenType.Identifier))
            return new VariableExpression(PreviousToken());

        if (CurrentMatches(TokenType.LeftParen))
        {
            var expr = Expression();
            Consume(TokenType.RightParen, "Expected a closing ')' after expression");
            return new GroupingExpression(expr);
        }

        throw Error(Peek(), "Not a valid expression start symbol");
    }

    List<Statement> Block()
    {
        List<Statement> statements = new();

        while (!CheckCurrent(TokenType.RightBrace) && !IsAtEnd())
            if (Declaration() is { } statement)
                statements.Add(statement);

        Consume(TokenType.RightBrace, "A block should end with '}'");
        return statements;
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

    void PanicAndSynchronize()
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
