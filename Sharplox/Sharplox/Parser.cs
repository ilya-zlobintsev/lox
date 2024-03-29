namespace Sharplox;

public class Parser(IReadOnlyList<Token> tokens)
{
    readonly static int MaxParamCount = 255;
    int _current;

    public List<Statement> Parse()
    {
        List<Statement> statements = [];
        while (!IsAtEnd())
            if (Declaration() is { } statement)
                statements.Add(statement);

        return statements;
    }

    Statement? Declaration()
    {
        try
        {
            if (CurrentMatches(TokenType.Class)) return ClassDeclaration();
            if (CurrentMatches(TokenType.Var)) return VariableDeclaration();
            return Statement();
        }
        catch (ParseError)
        {
            PanicAndSynchronize();
            return null;
        }
    }

    Statement ClassDeclaration()
    {
        var name = ConsumeToken(TokenType.Identifier, "Expected a valid class name");
        VariableExpression? superclass = null;
        if (CurrentMatches(TokenType.Less))
        {
            ConsumeToken(TokenType.Identifier, "Superclass name missing");
            superclass = new(PreviousToken());
        }

        ConsumeToken(TokenType.LeftBrace, "Expected a '{' before the class body");

        List<FunctionExpression> methods = [];
        while (!CheckCurrent(TokenType.RightBrace) && !IsAtEnd())
            methods.Add(Function("method"));

        ConsumeToken(TokenType.RightBrace, "Expected a '}' after the class body");
        return new ClassStatement(name, superclass, methods);
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
        if (CurrentMatches(TokenType.Break)) return BreakStatement();
        if (CurrentMatches(TokenType.Continue)) return ContinueStatement();
        if (CurrentMatches(TokenType.Return)) return ReturnStatement();
        if (CurrentMatches(TokenType.While)) return WhileStatement();
        if (CurrentMatches(TokenType.LeftBrace)) return BlockStatement();
        return ExpressionStatement();
    }

    Statement BreakStatement()
    {
        var keyword = PreviousToken();
        ConsumeToken(TokenType.Semicolon, $"Expected a ';' after break");
        return new BreakStatement(keyword);
    }

    Statement ContinueStatement()
    {
        var keyword = PreviousToken();
        ConsumeToken(TokenType.Semicolon, $"Expected a ';' after break");
        return new ContinueStatement(keyword);
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

        var condition = CheckCurrent(TokenType.Semicolon) ? new LiteralExpression(true) : Expression();

        ConsumeToken(TokenType.Semicolon, "Expected ';' after condition in 'for'");

        Expression? increment = null;

        if (!CheckCurrent(TokenType.RightParen))
            increment = Expression();

        ConsumeToken(TokenType.RightParen, "Expected ')' after 'for' clauses");

        var body = Statement();
        body = new LoopStatement(condition, body, increment);

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

        return new LoopStatement(condition, body, null);
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
        // Do not require a semicolon if the expression ends with a brace
        // This is required as function declarations are expressions, but they shouldn't be followed by a semicolon
        if (PreviousToken().Type != TokenType.RightBrace)
            ConsumeToken(TokenType.Semicolon, "Missing ';' after an expression");

        return new ExpressionStatement(value);
    }

    Expression Expression() => CurrentMatches(TokenType.Fun) ? Function("function") : Assignment();

    FunctionExpression Function(string kind)
    {
        var name = CheckCurrent(TokenType.Identifier) ? Advance() : null;
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

        return new FunctionExpression(name, parameters, body);
    }

    Expression Assignment()
    {
        var expr = Or();

        if (CurrentMatches(TokenType.Equal))
        {
            var equals = PreviousToken();
            var value = Assignment();

            switch (expr)
            {
                case VariableExpression var:
                    return new AssignmentExpression(var.Name, value);
                case GetExpression get:
                    return new SetExpression(get.Instance, get.Name, value);
                default:
                    Error(equals, "Invalid assignment target");
                    break;
            }
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
            else if (CurrentMatches(TokenType.Dot))
            {
                var name = ConsumeToken(TokenType.Identifier, "'.' should be followed by a property name");
                expr = new GetExpression(expr, name);
            }
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

        if (CurrentMatches(TokenType.Super))
        {
            var keyword = PreviousToken();
            ConsumeToken(TokenType.Dot, "Expected a '.' after 'super'");
            var method = ConsumeToken(TokenType.Identifier, "Expected a superclass method name");
            return new SuperExpression(keyword, method);
        }

        if (CurrentMatches(TokenType.This))
            return new ThisExpression(PreviousToken());

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

    class ParseError : Exception;
}
