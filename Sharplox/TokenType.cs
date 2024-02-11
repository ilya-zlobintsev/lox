namespace Sharplox;

public enum TokenType
{
    LeftParen, RightParen,
    LeftBrace, RightBrace,
    Comma, Dot,
    Minus, Plus,
    Semicolon,
    Slash,
    Star,
    
    Bang, BangEqual,
    Equal, EqualEqual,
    Greater, GreaterEqual,
    Less, LessEqual,
    
    Identifier,
    String,
    Number,
    
    Var,
    And, Or,
    If, Else,
    True, False,
    Fun, Class, This, Super,
    While, For,
    Return, Nil,
    Print,
    
    Eof,
}
