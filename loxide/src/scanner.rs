#[derive(Debug)]
pub struct Scanner<'a> {
    pub source: &'a str,
    start: usize,
    current: usize,
    line: u32,
}

impl<'a> Scanner<'a> {
    pub fn new(source: &'a str) -> Self {
        Self {
            source,
            start: 0,
            current: 0,
            line: 1,
        }
    }

    pub fn next_token(&mut self) -> Result<Token, ScannerError> {
        self.skip_whitespace();

        self.start = self.current;

        if self.is_at_end() {
            Ok(self.make_token(TokenType::Eof))
        } else {
            let c = self.advance();

            let token_type = match c {
                '(' => TokenType::LeftParen,
                ')' => TokenType::RightParen,
                '{' => TokenType::LeftBrace,
                '}' => TokenType::RightBrace,
                ';' => TokenType::Semicolon,
                ',' => TokenType::Comma,
                '.' => TokenType::Dot,
                '-' => TokenType::Minus,
                '+' => TokenType::Plus,
                '/' => TokenType::Slash,
                '*' => TokenType::Star,
                '!' => {
                    if self.current_matches('=') {
                        TokenType::BangEqual
                    } else {
                        TokenType::Bang
                    }
                }
                '=' => {
                    if self.current_matches('=') {
                        TokenType::EqualEqual
                    } else {
                        TokenType::Equal
                    }
                }
                '<' => {
                    if self.current_matches('=') {
                        TokenType::LessEqual
                    } else {
                        TokenType::Less
                    }
                }
                '>' => {
                    if self.current_matches('=') {
                        TokenType::GreaterEqual
                    } else {
                        TokenType::Greater
                    }
                }
                '"' => self.scan_string()?,
                n if n.is_ascii_digit() => self.scan_number(),
                c if c.is_ascii_alphabetic() || c == '_' => self.scan_identifier(),
                _ => {
                    return Err(self.error("Unexpected character".to_owned()));
                }
            };
            Ok(self.make_token(token_type))
        }
    }

    fn scan_string(&mut self) -> Result<TokenType, ScannerError> {
        while !self.is_at_end() && self.peek() != '"' {
            if self.peek() == '\n' {
                self.line += 1;
            }

            self.current += 1;
        }

        if !self.is_at_end() {
            self.current += 1;
            Ok(TokenType::String)
        } else {
            Err(self.error("Unterminated string literal".to_owned()))
        }
    }

    fn scan_number(&mut self) -> TokenType {
        while self.peek().is_ascii_digit() {
            self.current += 1;
        }

        if self.peek() == '.' && self.peek_next().is_ascii_digit() {
            while self.peek().is_ascii_digit() {
                self.current += 1;
            }
        }

        TokenType::Number
    }

    fn scan_identifier(&mut self) -> TokenType {
        loop {
            let current = self.peek();
            if current.is_alphanumeric() || current == '_' {
                self.current += 1;
            } else {
                break;
            }
        }

        match self.source.as_bytes()[self.start] {
            b'a' => self.check_keyword(1, "nd", TokenType::And),
            b'c' => self.check_keyword(1, "lass", TokenType::Class),
            b'e' => self.check_keyword(1, "lse", TokenType::Else),
            b'f' => {
                if self.current - self.start > 1 {
                    match self.source.as_bytes()[self.start + 1] {
                        b'a' => self.check_keyword(2, "lse", TokenType::False),
                        b'o' => self.check_keyword(2, "r", TokenType::For),
                        b'u' => self.check_keyword(2, "n", TokenType::Fun),
                        _ => TokenType::Identifier,
                    }
                } else {
                    TokenType::Identifier
                }
            }
            b'i' => self.check_keyword(1, "f", TokenType::If),
            b'n' => self.check_keyword(1, "il", TokenType::Nil),
            b'o' => self.check_keyword(1, "r", TokenType::Or),
            b'p' => self.check_keyword(1, "rint", TokenType::Print),
            b'r' => self.check_keyword(1, "eturn", TokenType::Return),
            b's' => self.check_keyword(1, "uper", TokenType::Super),
            b't' => {
                if self.current - self.start > 1 {
                    match self.source.as_bytes()[self.start + 1] {
                        b'h' => self.check_keyword(2, "is", TokenType::This),
                        b'r' => self.check_keyword(2, "ue", TokenType::True),
                        _ => TokenType::Identifier,
                    }
                } else {
                    TokenType::Identifier
                }
            }
            b'v' => self.check_keyword(1, "ar", TokenType::Var),
            b'w' => self.check_keyword(1, "hile", TokenType::While),
            _ => TokenType::Identifier,
        }
    }

    fn check_keyword(&self, start: usize, rest: &str, token_type: TokenType) -> TokenType {
        if self.current - self.start == start + rest.len()
            && &self.source[self.start + start..self.start + start + rest.len()] == rest
        {
            token_type
        } else {
            TokenType::Identifier
        }
    }

    fn make_token(&self, token_type: TokenType) -> Token {
        Token {
            token_type,
            start: self.start,
            end: self.current,
            line: self.line,
        }
    }

    fn error(&self, message: String) -> ScannerError {
        ScannerError {
            message,
            line: self.line,
            start: self.start,
            end: self.current,
        }
    }

    fn is_at_end(&self) -> bool {
        self.current >= self.source.len()
    }

    fn advance(&mut self) -> char {
        let c = self.peek();
        self.current += 1;
        c
    }

    fn peek(&self) -> char {
        // TODO: handle non-ascii
        *self.source.as_bytes().get(self.current).unwrap_or(&b'\0') as char
    }

    fn peek_next(&self) -> char {
        // TODO: handle non-ascii
        *self
            .source
            .as_bytes()
            .get(self.current + 1)
            .unwrap_or(&b'\0') as char
    }

    fn skip_whitespace(&mut self) {
        loop {
            if self.is_at_end() {
                break;
            }

            match self.peek() {
                '\t' | '\r' | ' ' => self.current += 1,
                '\n' => {
                    self.line += 1;
                    self.current += 1;
                }
                '/' => {
                    if self.peek_next() == '/' {
                        while !self.is_at_end() && self.peek() != '\n' {
                            self.current += 1;
                        }
                    } else {
                        break;
                    }
                }
                _ => break,
            }
        }
    }

    fn current_matches(&mut self, expected: char) -> bool {
        if self.is_at_end() {
            return false;
        }
        if self.peek() != expected {
            return false;
        }
        self.current += 1;
        true
    }

    pub fn lexeme(&self, token: Token) -> &str {
        &self.source[token.start..token.end]
    }
}

#[derive(Clone, Copy)]
pub struct Token {
    pub token_type: TokenType,
    pub start: usize,
    pub end: usize,
    pub line: u32,
}

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum TokenType {
    LeftParen,
    RightParen,
    LeftBrace,
    RightBrace,
    Comma,
    Dot,
    Minus,
    Plus,
    Semicolon,
    Slash,
    Star,

    Bang,
    BangEqual,
    Equal,
    EqualEqual,
    Greater,
    GreaterEqual,
    Less,
    LessEqual,

    Identifier,
    String,
    Number,

    Var,
    And,
    Or,
    If,
    Else,
    True,
    False,
    Fun,
    Class,
    This,
    Super,
    While,
    For,
    Return,
    Nil,
    Print,

    Eof,
}

#[derive(Debug)]
pub struct ScannerError {
    pub message: String,
    pub line: u32,
    pub start: usize,
    pub end: usize,
}

#[cfg(test)]
mod tests {
    use super::Scanner;
    use crate::scanner::TokenType;

    #[test]
    fn scan_and() {
        let mut scanner = Scanner::new("and");
        assert_eq!(TokenType::And, scanner.next_token().unwrap().token_type);
        assert_eq!(TokenType::Eof, scanner.next_token().unwrap().token_type);

        let mut scanner = Scanner::new("anda");
        assert_eq!(
            TokenType::Identifier,
            scanner.next_token().unwrap().token_type
        );
        assert_eq!(TokenType::Eof, scanner.next_token().unwrap().token_type);
    }

    #[test]
    fn scan_keyword() {
        let mut scanner = Scanner::new("for while true");
        assert_eq!(TokenType::For, scanner.next_token().unwrap().token_type);
        assert_eq!(TokenType::While, scanner.next_token().unwrap().token_type);
        assert_eq!(TokenType::True, scanner.next_token().unwrap().token_type);
    }

    #[test]
    fn scan_string_literal() {
        let mut scanner = Scanner::new("\"hello\"");
        assert_eq!(TokenType::String, scanner.next_token().unwrap().token_type);
        assert_eq!(TokenType::Eof, scanner.next_token().unwrap().token_type);
    }

    #[test]
    fn basic_program() {
        let source = r#"
for (var i = 1; i <= 10; i = i + 1) {
    print "Current number is: ";
    print i;
}
        "#;

        let mut scanner = Scanner::new(source);
        let mut token_types = Vec::new();
        loop {
            let token = scanner.next_token().unwrap();
            if token.token_type == TokenType::Eof {
                break;
            }
            token_types.push(token.token_type);
        }

        use TokenType::*;
        let expected_token_types = vec![
            For, LeftParen, Var, Identifier, Equal, Number, Semicolon, Identifier, LessEqual,
            Number, Semicolon, Identifier, Equal, Identifier, Plus, Number, RightParen, LeftBrace,
            Print, String, Semicolon, Print, Identifier, Semicolon, RightBrace,
        ];

        assert_eq!(expected_token_types, token_types);
    }
}
