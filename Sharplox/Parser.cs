using System.Runtime.InteropServices.JavaScript;

namespace Sharplox;

public class Parser(IReadOnlyList<Token> tokens)
{
    private readonly static int MaxParamCount = 255;
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
            if (CurrentMatches(TokenType.Fun)) return Function("function");
            if (CurrentMatches(TokenType.Var)) return VariableDeclaration();
            return Statement();
        }
        catch (ParseError)
        {
            PanicAndSynchronize();
            return null;
        }
    }

    Statement Function(string kind)
    {
        var name = ConsumeToken(TokenType.Identifier, $"Expected a {kind} name");
        ConsumeToken(TokenType.LeftParen, $"Expected a '(' after {kind} name");
        List<Token> parameters = [];

        if (!CheckCurrent(TokenType.RightParen))
        {
            do
            {
                if (parameters.Count >= MaxParamCount)
                    Error(Peek(), $"Cannot have more than {MaxParamCount} parameters");

                var param = ConsumeToken(TokenType.Identifier, "Expected a parameter name");
                parameters.Add(param);
            } while (CurrentMatches(TokenType.Comma));
        }

        ConsumeToken(TokenType.RightParen, "Expected a ')' after parameters.");
        ConsumeToken(TokenType.LeftBrace, $"Expected the {kind} body to start with a '{{'");
        var body = Block();

        return new FunctionStatement(name, parameters, body);
    }

    Statement VariableDeclaration()
    {
        var name = ConsumeToken(TokenType.Identifier, "Expected a valid variable name");
        Expression? initializer = null;
        if (CurrentMatches(TokenType.Equal))
            initializer = Expression();

        ConsumeToken(TokenType.Semicolon, "Expected a ';' after a variable declaration");
        return new VariableStatement(name, initializer);
    }

    Statement Statement()
    {
        if (CurrentMatches(TokenType.For)) return ForStatement();
        if (CurrentMatches(TokenType.If)) return IfStatement();
        if (CurrentMatches(TokenType.Print)) return PrintStatement();
        if (CurrentMatches(TokenType.Return)) return ReturnStatement();
        if (CurrentMatches(TokenType.While)) return WhileStatement();
        if (CurrentMatches(TokenType.LeftBrace)) return BlockStatement();
        return ExpressionStatement();
    }

    Statement BlockStatement() => new BlockStatement(Block());

    Statement ForStatement()
    {
        ConsumeToken(TokenType.LeftParen, "Expected '(' after 'for'");

        Statement? initializer = null;
        if (CurrentMatches(TokenType.Var))
            initializer = VariableDeclaration();
        else if (!CurrentMatches(TokenType.Semicolon))
            initializer = ExpressionStatement();

        Expression? condition = null;
        if (!CheckCurrent(TokenType.Semicolon))
            condition = Expression();

        ConsumeToken(TokenType.Semicolon, "Expected ';' after condition in 'for'");

        Expression? increment = null;

        if (!CheckCurrent(TokenType.RightParen))
            increment = Expression();

        ConsumeToken(TokenType.RightParen, "Expected ')' after 'for' clauses");

        var body = Statement();

        if (increment is not null)
            body = new BlockStatement([body, new ExpressionStatement(increment)]);

        body = new WhileStatement(condition ?? new LiteralExpression(true), body);

        if (initializer is not null)
            body = new BlockStatement([initializer, body]);

        return body;
    }

    Statement IfStatement()
    {
        ConsumeToken(TokenType.LeftParen, "If should be followed by '('");
        var condition = Expression();
        ConsumeToken(TokenType.RightParen, "Expected ')' after condition");

        var thenBranch = Statement();
        var elseBranch = CurrentMatches(TokenType.Else) ? Statement() : null;

        return new IfStatement(condition, thenBranch, elseBranch);
    }

    Statement WhileStatement()
    {
        ConsumeToken(TokenType.LeftParen, "Expected '(' after 'while'");
        var condition = Expression();
        ConsumeToken(TokenType.RightParen, "Expected ')' after condition in 'while'");
        var body = Statement();

        return new WhileStatement(condition, body);
    }

    Statement ReturnStatement()
    {
        var keyword = PreviousToken();
        Expression? value = null;
        if (!CheckCurrent(TokenType.Semicolon))
            value = Expression();

        ConsumeToken(TokenType.Semicolon, $"Expected a ';' after the return value");
        return new ReturnStatement(keyword, value);
    }

    Statement PrintStatement()
    {
        var value = Expression();
        ConsumeToken(TokenType.Semicolon, "Missing ';' after a value");
        return new PrintStatement(value);
    }

    Statement ExpressionStatement()
    {
        var value = Expression();
        ConsumeToken(TokenType.Semicolon, "Missing ';' after an expression");
        return new ExpressionStatement(value);
    }

    Expression Expression() => Assignment();

    Expression Assignment()
    {
        var expr = Or();

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

    Expression Or()
    {
        var expr = And();

        while (CurrentMatches(TokenType.Or))
        {
            var op = PreviousToken();
            var right = And();
            expr = new LogicalExpression(expr, op, right);
        }

        return expr;
    }

    Expression And()
    {
        var expr = Equality();

        while (CurrentMatches(TokenType.And))
        {
            var op = PreviousToken();
            var right = Equality();
            expr = new LogicalExpression(expr, op, right);
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

        return Call();
    }

    Expression Call()
    {
        var expr = Primary();

        while (true)
        {
            if (CurrentMatches(TokenType.LeftParen))
                expr = FinishCall(expr);
            else
                break;
        }

        return expr;
    }

    Expression FinishCall(Expression callee)
    {
        List<Expression> arguments = new();
        if (!CheckCurrent(TokenType.RightParen))
        {
            do
            {
                if (arguments.Count >= MaxParamCount)
                {
                    Error(Peek(), $"Functions have a maximum of {MaxParamCount} arguments");
                }

                arguments.Add(Expression());
            } while (CurrentMatches(TokenType.Comma));
        }

        var paren = ConsumeToken(TokenType.RightParen, "Function arguments should be followed by a ')'");

        return new CallExpression(callee, paren, arguments);
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
            ConsumeToken(TokenType.RightParen, "Expected a closing ')' after expression");
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

        ConsumeToken(TokenType.RightBrace, "A block should end with '}'");
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

    Token ConsumeToken(TokenType type, string message) => CheckCurrent(type) ? Advance() : throw Error(Peek(), message);

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
