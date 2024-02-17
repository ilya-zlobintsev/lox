use crate::{
    chunk::Chunk,
    op_code::OpCode,
    scanner::{Scanner, Token, TokenType},
    value::Value,
};
use std::ops::Range;

pub fn compile(source: &str) -> Option<Chunk> {
    let scanner = Scanner::new(source);

    let parser = Parser {
        scanner,
        current: None,
        previous: None,
        had_error: false,
        panic_mode: false,
    };

    let mut compiler = Compiler {
        compiling_chunk: Chunk::default(),
        parser,
    };

    compiler.parser.advance();
    compiler.expression();
    compiler
        .parser
        .consume(TokenType::Eof, "Expected end of expression");

    compiler.end()
}

struct Compiler<'a> {
    compiling_chunk: Chunk,
    parser: Parser<'a>,
}

impl<'a> Compiler<'a> {
    fn current_chunk(&mut self) -> &mut Chunk {
        &mut self.compiling_chunk
    }

    fn emit_byte(&mut self, byte: impl Into<u8>) {
        let line = self.parser.previous.unwrap().line;
        self.current_chunk().write(byte.into(), line);
    }

    fn emit_bytes(&mut self, byte_1: impl Into<u8>, byte_2: impl Into<u8>) {
        let line = self.parser.previous.unwrap().line;
        self.current_chunk()
            .write_slice(&[byte_1.into(), byte_2.into()], line);
    }

    fn emit_return(&mut self) {
        self.emit_byte(OpCode::Return.as_byte());
    }

    fn emit_constant(&mut self, value: Value) {
        let index = self.make_constant(value);
        self.emit_bytes(OpCode::Constant, index);
    }

    fn make_constant(&mut self, value: Value) -> u8 {
        let index = self.current_chunk().add_constant(value);
        if index > u8::MAX as usize {
            self.parser.error("Too many constants in one chunk");
            0
        } else {
            index as u8
        }
    }

    fn end(mut self) -> Option<Chunk> {
        self.emit_return();

        if self.parser.had_error {
            None
        } else {
            #[cfg(feature = "print")]
            self.compiling_chunk.disassemble("code");

            Some(self.compiling_chunk)
        }
    }

    fn expression(&mut self) {
        self.parse_presedence(Precedence::Assignment);
    }

    fn number(&mut self) {
        let lexeme = self.parser.scanner.lexeme(self.parser.previous.unwrap());
        match lexeme.parse::<f64>() {
            Ok(value) => self.emit_constant(Value::Number(value)),
            Err(_) => self.parser.error("Could not parse number"),
        }
    }

    fn unary(&mut self) {
        let operator_type = self.parser.previous.unwrap().token_type;

        self.parse_presedence(Precedence::Unary);

        match operator_type {
            TokenType::Minus => self.emit_byte(OpCode::Negate),
            TokenType::Bang => self.emit_byte(OpCode::Not),
            _ => (),
        }
    }

    fn binary(&mut self) {
        let operator_type = self.previous_token_type();
        let rule = self.get_rule(operator_type);
        self.parse_presedence(Precedence::from_byte(rule.precedence.as_byte() + 1).unwrap());

        match operator_type {
            TokenType::Plus => self.emit_byte(OpCode::Add),
            TokenType::Minus => self.emit_byte(OpCode::Subtract),
            TokenType::Star => self.emit_byte(OpCode::Multiply),
            TokenType::Slash => self.emit_byte(OpCode::Divide),
            TokenType::BangEqual => self.emit_bytes(OpCode::Equal, OpCode::Not),
            TokenType::EqualEqual => self.emit_byte(OpCode::Equal),
            TokenType::Greater => self.emit_byte(OpCode::Greater),
            TokenType::GreaterEqual => self.emit_bytes(OpCode::Less, OpCode::Not),
            TokenType::Less => self.emit_byte(OpCode::Less),
            TokenType::LessEqual => self.emit_bytes(OpCode::Greater, OpCode::Not),
            _ => (),
        }
    }

    fn grouping(&mut self) {
        self.expression();
        self.parser
            .consume(TokenType::RightParen, "Expected a ')' after expression");
    }

    fn literal(&mut self) {
        match self.previous_token_type() {
            TokenType::False => self.emit_byte(OpCode::False),
            TokenType::True => self.emit_byte(OpCode::True),
            TokenType::Nil => self.emit_byte(OpCode::Nil),
            _ => (),
        }
    }

    fn string(&mut self) {
        let previous_token = self.parser.previous.unwrap();
        let value = &self.parser.scanner.source[previous_token.start + 1..previous_token.end - 1];
        self.emit_constant(Value::new_string(value));
    }

    fn parse_presedence(&mut self, precedence: Precedence) {
        self.parser.advance();
        let prefix_rule = self.get_rule(self.previous_token_type());

        match prefix_rule.prefix {
            Some(prefix_rule) => prefix_rule(self),
            None => self.parser.error("Expected an expression"),
        }

        while precedence <= self.get_rule(self.current_token_type()).precedence {
            self.parser.advance();
            let infix_rule = self.get_rule(self.previous_token_type()).infix.unwrap();
            infix_rule(self);
        }
    }

    fn get_rule<'comp>(&'comp mut self, token_type: TokenType) -> ParseRule<'a, 'comp> {
        use TokenType::*;
        match token_type {
            LeftParen => ParseRule::new(Some(Self::grouping), None, Precedence::None),
            Minus => ParseRule::new(Some(Self::unary), Some(Self::binary), Precedence::Term),
            Plus => ParseRule::new(None, Some(Self::binary), Precedence::Term),
            Slash => ParseRule::new(None, Some(Self::binary), Precedence::Factor),
            Star => ParseRule::new(None, Some(Self::binary), Precedence::Factor),
            Number => ParseRule::new(Some(Self::number), None, Precedence::None),
            False => ParseRule::new(Some(Self::literal), None, Precedence::None),
            True => ParseRule::new(Some(Self::literal), None, Precedence::None),
            Nil => ParseRule::new(Some(Self::literal), None, Precedence::None),
            Bang => ParseRule::new(Some(Self::unary), None, Precedence::None),
            BangEqual => ParseRule::new(None, Some(Self::binary), Precedence::Equality),
            EqualEqual => ParseRule::new(None, Some(Self::binary), Precedence::Equality),
            Greater => ParseRule::new(None, Some(Self::binary), Precedence::Comparison),
            GreaterEqual => ParseRule::new(None, Some(Self::binary), Precedence::Comparison),
            Less => ParseRule::new(None, Some(Self::binary), Precedence::Comparison),
            LessEqual => ParseRule::new(None, Some(Self::binary), Precedence::Comparison),
            String => ParseRule::new(Some(Self::string), None, Precedence::None),
            _ => ParseRule::new(None, None, Precedence::None),
        }
    }

    fn current_token_type(&self) -> TokenType {
        self.parser.current.unwrap().token_type
    }

    fn previous_token_type(&self) -> TokenType {
        self.parser.previous.unwrap().token_type
    }
}

struct ParseRule<'src, 'comp> {
    prefix: Option<fn(&'comp mut Compiler<'src>)>,
    infix: Option<fn(&'comp mut Compiler<'src>)>,
    precedence: Precedence,
}

impl<'src, 'comp> ParseRule<'src, 'comp> {
    fn new(
        prefix: Option<fn(&'comp mut Compiler<'src>)>,
        infix: Option<fn(&'comp mut Compiler<'src>)>,
        precedence: Precedence,
    ) -> Self {
        Self {
            prefix,
            infix,
            precedence,
        }
    }
}

struct Parser<'a> {
    scanner: Scanner<'a>,
    current: Option<Token>,
    previous: Option<Token>,
    had_error: bool,
    panic_mode: bool,
}

impl<'a> Parser<'a> {
    fn advance(&mut self) {
        self.previous = self.current.take();

        loop {
            match self.scanner.next_token() {
                Ok(token) => {
                    self.current = Some(token);
                    break;
                }
                Err(err) => {
                    self.error_at(Some(err.start..err.end), err.line, &err.message);
                }
            }
        }
    }

    fn consume(&mut self, expected_type: TokenType, message: &str) {
        if self
            .current
            .is_some_and(|current| current.token_type == expected_type)
        {
            self.advance();
        } else {
            self.error_at_current(message);
        }
    }

    fn error_at_current(&mut self, message: &str) {
        let current = self.current.unwrap();
        self.error_at(Some(current.start..current.end), current.line, message);
    }

    fn error(&mut self, message: &str) {
        let previous = self.previous.unwrap();
        self.error_at(Some(previous.start..previous.end), previous.line, message);
    }

    fn error_at(&mut self, range: Option<Range<usize>>, line: u32, message: &str) {
        if self.panic_mode {
            return;
        }

        self.panic_mode = true;
        eprint!("[line {line}] Error");

        if let Some(range) = range {
            eprint!(" at '{}'", &self.scanner.source[range]);
        } else {
            eprint!(" at end");
        }

        eprintln!(": {message}");
        self.had_error = true;
    }
}

// #[derive(Debug)]
// pub enum CompileError {
//     Scanner(ScannerError),
// }

// impl From<ScannerError> for CompileError {
//     fn from(value: ScannerError) -> Self {
//         Self::Scanner(value)
//     }
// }

convertable_enum! {
    Precedence,
    None = 0,
    Assignment = 1,
    Or = 2,
    And = 3,
    Equality = 4,
    Comparison = 5,
    Term = 6,
    Factor = 7,
    Unary = 8,
    Call = 9 ,
    Primary = 10,
}
