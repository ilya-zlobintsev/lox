use crate::scanner::{Scanner, TokenType};

pub fn compile(source: &str) {
    let mut scanner = Scanner::new(source);

    let mut line = 0;
    loop {
        let token = scanner.next_token().unwrap();
        if token.line != line {
            print!("{:4} ", token.line);
            line = token.line;
        } else {
            print!("   | ");
        }
        println!(
            "{:?} '{}'",
            token.token_type,
            &source[token.start..token.end]
        );

        if token.token_type == TokenType::Eof {
            break;
        }
    }
}
