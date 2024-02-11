namespace Sharplox;

public class Lexer(string source)
{
    readonly List<Token> _tokens = new();
    int _start;
    int _current;
    int _line = 1;

    char Advance() => source[_current++];
    bool IsAtEnd() => _current >= source.Length;

    bool NextMatches(char expected)
    {
        if (IsAtEnd()) return false;
        if (source[_current] != expected) return false;

        _current++;
        return true;
    }

    char Peek() => IsAtEnd() ? '\0' : source[_current];
    char PeekNext() => _current + 1 >= source.Length ? '\0' : source[_current + 1];

    public List<Token> ScanTokens()
    {
        while (!IsAtEnd())
        {
            _start = _current;
            ScanToken();
        }

        _tokens.Add(new(TokenType.Eof, "", null, _line));
        return _tokens;
    }

    void ScanToken()
    {
        var c = Advance();

        switch (c)
        {
            case '(':
                AddToken(TokenType.LeftParen);
                break;
            case ')':
                AddToken(TokenType.RightParen);
                break;
            case '{':
                AddToken(TokenType.LeftBrace);
                break;
            case '}':
                AddToken(TokenType.RightBrace);
                break;
            case ',':
                AddToken(TokenType.Comma);
                break;
            case '.':
                AddToken(TokenType.Dot);
                break;
            case '-':
                AddToken(TokenType.Minus);
                break;
            case '+':
                AddToken(TokenType.Plus);
                break;
            case ';':
                AddToken(TokenType.Semicolon);
                break;
            case '*':
                AddToken(TokenType.Star);
                break;
            case '!':
                AddToken(NextMatches('=') ? TokenType.BangEqual : TokenType.Equal);
                break;
            case '=':
                AddToken(NextMatches('=') ? TokenType.EqualEqual : TokenType.Equal);
                break;
            case '<':
                AddToken(NextMatches('=') ? TokenType.LessEqual : TokenType.Less);
                break;
            case '>':
                AddToken(NextMatches('=') ? TokenType.GreaterEqual : TokenType.Greater);
                break;
            case '/':
                if (NextMatches('/'))
                    while (Peek() != '\n' && !IsAtEnd())
                        Advance();
                else
                    AddToken(TokenType.Slash);

                break;
            case ' ':
            case '\r':
            case '\t':
                // Ignore whitespace.
                break;
            case '\n':
                _line++;
                break;
            case '"':
                StringLiteral();
                break;
            default:
                if (char.IsAsciiDigit(c))
                    NumberLiteral();
                else if (char.IsAsciiLetterOrDigit(c))
                    Identifier();
                else
                    Lox.Error(_line, $"Unexpected token `{c}`");

                break;
        }
    }

    void AddToken(TokenType type, object? literal = null)
    {
        var text = source.Substring(_start, _current - _start);
        _tokens.Add(new(type, text, literal, _line));
    }

    void StringLiteral()
    {
        while (Peek() != '"' && !IsAtEnd())
        {
            if (Peek() == '\n') _line++;
            Advance();
        }

        if (IsAtEnd())
        {
            Lox.Error(_line, "Unexpected end of file while reading a string");
            return;
        }

        Advance();

        var value = source.Substring(_start + 1, _current - _start - 2);
        AddToken(TokenType.String, value);
    }

    void NumberLiteral()
    {
        while (char.IsAsciiDigit(Peek())) Advance();

        if (Peek() == '.' && char.IsAsciiDigit(PeekNext()))
        {
            Advance();
            while (char.IsAsciiDigit(Peek())) Advance();
        }

        var numberSlice = source.Substring(_start, _current - _start);
        var value = double.Parse(numberSlice);
        AddToken(TokenType.Number, value);
    }

    void Identifier()
    {
        while (char.IsAsciiLetterOrDigit(Peek())) Advance();
        var tokenValue = source.Substring(_start, _current - _start);
        var tokenType = tokenValue switch
        {
            "var" => TokenType.Var,
            "and" => TokenType.And,
            "or" => TokenType.Or,
            "if" => TokenType.If,
            "else" => TokenType.Else,
            "while" => TokenType.While,
            "for" => TokenType.For,
            "true" => TokenType.True,
            "false" => TokenType.False,
            "fun" => TokenType.Fun,
            "class" => TokenType.Class,
            "this" => TokenType.This,
            "super" => TokenType.Super,
            "return" => TokenType.Return,
            "nil" => TokenType.Nil,
            "print" => TokenType.Print,
            _ => TokenType.Identifier,
        };

        AddToken(tokenType);
    }
}
