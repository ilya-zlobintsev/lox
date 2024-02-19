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
        locals: Vec::new(),
        scope_depth: 0,
    };

    compiler.parser.advance();

    while !compiler.match_token(TokenType::Eof) {
        compiler.declaration();
    }

    compiler
        .parser
        .consume(TokenType::Eof, "Expected end of expression");

    compiler.end()
}

struct Compiler<'src> {
    compiling_chunk: Chunk,
    parser: Parser<'src>,
    locals: Vec<Local>,
    scope_depth: i32,
}

struct Local {
    name: Token,
    depth: i32,
}

impl<'src> Compiler<'src> {
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

    fn match_token(&mut self, expected_type: TokenType) -> bool {
        if self.check_current_token(expected_type) {
            self.parser.advance();
            true
        } else {
            false
        }
    }

    fn check_current_token(&self, expected_type: TokenType) -> bool {
        self.parser
            .current
            .is_some_and(|token| token.token_type == expected_type)
    }

    fn expression(&mut self) {
        self.parse_presedence(Precedence::Assignment);
    }

    fn declaration(&mut self) {
        if self.match_token(TokenType::Var) {
            self.var_declaration();
        } else {
            self.statement();
        }

        if self.parser.panic_mode {
            self.synchronize();
        }
    }

    fn var_declaration(&mut self) {
        let global = self.parse_variable("Expected a variable name");

        if self.match_token(TokenType::Equal) {
            self.expression();
        } else {
            self.emit_byte(OpCode::Nil);
        }
        self.parser.consume(
            TokenType::Semicolon,
            "Expected a ';' after variable declaration",
        );

        self.define_variable(global);
    }

    fn define_variable(&mut self, var_index: u8) {
        if self.scope_depth == 0 {
            self.emit_bytes(OpCode::DefineGlobal, var_index);
        } else {
            self.mark_initialized();
        }
    }

    fn mark_initialized(&mut self) {
        self.locals.last_mut().unwrap().depth = self.scope_depth;
    }

    fn declare_variable(&mut self) {
        if self.scope_depth != 0 {
            let name = self.parser.previous.unwrap();

            for local in self.locals.iter().rev() {
                if local.depth != -1 && local.depth < self.scope_depth {
                    break;
                }

                if self.identifiers_eq(name, local.name) {
                    self.parser
                        .error("Variable with this name already exists in the current scope");
                }
            }

            self.add_local(name);
        }
    }

    fn identifiers_eq(&self, a: Token, b: Token) -> bool {
        self.parser.scanner.source[a.start..a.end] == self.parser.scanner.source[b.start..b.end]
    }

    fn add_local(&mut self, name: Token) {
        let local = Local { name, depth: -1 };
        self.locals.push(local);
    }

    fn parse_variable(&mut self, message: &str) -> u8 {
        self.parser.consume(TokenType::Identifier, message);

        self.declare_variable();
        if self.scope_depth > 0 {
            0
        } else {
            self.identifier_constant(self.parser.previous.unwrap())
        }
    }

    fn identifier_constant(&mut self, name: Token) -> u8 {
        let name = &self.parser.scanner.source[name.start..name.end];
        self.make_constant(Value::new_string(name))
    }

    fn statement(&mut self) {
        if self.match_token(TokenType::Print) {
            self.print_statement();
        } else if self.match_token(TokenType::LeftBrace) {
            self.begin_scope();
            self.block();
            self.end_scope();
        } else {
            self.expression_statement();
        }
    }

    fn block(&mut self) {
        while !self.is_at_end() && !self.check_current_token(TokenType::RightBrace) {
            self.declaration();
        }

        self.parser
            .consume(TokenType::RightBrace, "Exepected a '}' after block");
    }

    fn begin_scope(&mut self) {
        self.scope_depth += 1;
    }

    fn end_scope(&mut self) {
        self.scope_depth -= 1;

        while self
            .locals
            .last()
            .is_some_and(|top_var| top_var.depth > self.scope_depth)
        {
            self.locals.pop().unwrap();
            self.emit_byte(OpCode::Pop);
        }
    }

    fn expression_statement(&mut self) {
        self.expression();
        self.parser
            .consume(TokenType::Semicolon, "Expected a ';' after expression");
        self.emit_byte(OpCode::Pop);
    }

    fn print_statement(&mut self) {
        self.expression();
        self.parser
            .consume(TokenType::Semicolon, "Expected a ';' after value");
        self.emit_byte(OpCode::Print);
    }

    fn number(&mut self, _can_assign: bool) {
        let lexeme = self.parser.scanner.lexeme(self.parser.previous.unwrap());
        match lexeme.parse::<f64>() {
            Ok(value) => self.emit_constant(Value::Number(value)),
            Err(_) => self.parser.error("Could not parse number"),
        }
    }

    fn unary(&mut self, _can_assign: bool) {
        let operator_type = self.parser.previous.unwrap().token_type;

        self.parse_presedence(Precedence::Unary);

        match operator_type {
            TokenType::Minus => self.emit_byte(OpCode::Negate),
            TokenType::Bang => self.emit_byte(OpCode::Not),
            _ => (),
        }
    }

    fn binary(&mut self, _can_assign: bool) {
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

    fn grouping(&mut self, _can_assign: bool) {
        self.expression();
        self.parser
            .consume(TokenType::RightParen, "Expected a ')' after expression");
    }

    fn literal(&mut self, _can_assign: bool) {
        match self.previous_token_type() {
            TokenType::False => self.emit_byte(OpCode::False),
            TokenType::True => self.emit_byte(OpCode::True),
            TokenType::Nil => self.emit_byte(OpCode::Nil),
            _ => (),
        }
    }

    fn string(&mut self, _can_assign: bool) {
        let previous_token = self.parser.previous.unwrap();
        let value = &self.parser.scanner.source[previous_token.start + 1..previous_token.end - 1];
        self.emit_constant(Value::new_string(value));
    }

    fn variable(&mut self, can_assign: bool) {
        self.named_variable(self.parser.previous.unwrap(), can_assign);
    }

    fn resolve_local(&mut self, name: Token) -> Option<u8> {
        self.locals
            .iter()
            .enumerate()
            .rev()
            .find(|(_, local)| self.identifiers_eq(name, local.name))
            .map(|(i, local)| {
                if local.depth == -1 {
                    self.parser
                        .error("Cannot read local variable in its own initializer");
                }
                i as u8
            })
    }

    fn named_variable(&mut self, name: Token, can_assign: bool) {
        let (get_op, set_op, arg) = match self.resolve_local(name) {
            None => {
                let arg = self.identifier_constant(name);
                (OpCode::GetGlobal, OpCode::SetGlobal, arg)
            }
            Some(local) => (OpCode::GetLocal, OpCode::SetLocal, local),
        };

        if can_assign && self.match_token(TokenType::Equal) {
            self.expression();
            self.emit_bytes(set_op, arg);
        } else {
            self.emit_bytes(get_op, arg);
        }
    }

    fn parse_presedence(&mut self, precedence: Precedence) {
        self.parser.advance();
        let prefix_rule = self.get_rule(self.previous_token_type());
        let can_assign = precedence <= Precedence::Assignment;

        match prefix_rule.prefix {
            Some(prefix_rule) => {
                prefix_rule(self, can_assign);
            }
            None => self.parser.error("Expected an expression"),
        }

        if can_assign && self.match_token(TokenType::Equal) {
            self.parser.error("Invalid assignment target");
        }

        while precedence <= self.get_rule(self.current_token_type()).precedence {
            self.parser.advance();
            let infix_rule = self.get_rule(self.previous_token_type()).infix.unwrap();
            infix_rule(self, can_assign);
        }
    }

    fn get_rule<'comp>(&'comp mut self, token_type: TokenType) -> ParseRule<'src, 'comp> {
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
            Identifier => ParseRule::new(Some(Self::variable), None, Precedence::None),
            _ => ParseRule::new(None, None, Precedence::None),
        }
    }

    fn current_token_type(&self) -> TokenType {
        self.parser.current.unwrap().token_type
    }

    fn previous_token_type(&self) -> TokenType {
        self.parser.previous.unwrap().token_type
    }

    fn synchronize(&mut self) {
        self.parser.panic_mode = false;

        while self.parser.current.unwrap().token_type != TokenType::Eof {
            if self.parser.previous.unwrap().token_type == TokenType::Semicolon {
                return;
            }

            use TokenType::*;
            match self.parser.current.unwrap().token_type {
                Class | Fun | Var | For | If | While | Print | Return => return,
                _ => (),
            }

            self.parser.advance();
        }
    }

    fn is_at_end(&self) -> bool {
        self.parser.scanner.is_at_end()
    }
}

type ParseFn<'comp, 'src> = fn(&'comp mut Compiler<'src>, bool);

struct ParseRule<'src, 'comp> {
    prefix: Option<ParseFn<'comp, 'src>>,
    infix: Option<ParseFn<'comp, 'src>>,
    precedence: Precedence,
}

impl<'src, 'comp> ParseRule<'src, 'comp> {
    fn new(
        prefix: Option<ParseFn<'comp, 'src>>,
        infix: Option<ParseFn<'comp, 'src>>,
        precedence: Precedence,
    ) -> Self {
        Self {
            prefix,
            infix,
            precedence,
        }
    }
}

struct Parser<'src> {
    scanner: Scanner<'src>,
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
