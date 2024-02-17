mod chunk;
#[macro_use]
mod macros;
mod compiler;
mod object;
mod op_code;
mod scanner;
mod value;
mod vm;

use crate::vm::Vm;
use std::{
    env, fs,
    io::{stdin, stdout, Write},
};

fn main() {
    let mut args = env::args().skip(1);

    if let Some(file_path) = args.next() {
        run_file(&file_path)
    } else {
        repl()
    }
}

fn run_file(path: &str) {
    let source = fs::read_to_string(path).unwrap();
    let result = Vm::interpret(&source);
    println!("{result:?}");
}

fn repl() {
    let mut stdout = stdout();
    print!("> ");
    stdout.flush().unwrap();

    for line in stdin().lines() {
        let line = line.unwrap();
        let result = Vm::interpret(&line);
        println!("{result:?}");

        print!("> ");
        stdout.flush().unwrap();
    }
}
