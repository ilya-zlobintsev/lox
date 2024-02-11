namespace Sharplox;

public record Token(TokenType Type, string Lexeme, object? Literal, int Line)
{
    public override string ToString() => $"{Type} `{Lexeme}` {Literal}";
}
