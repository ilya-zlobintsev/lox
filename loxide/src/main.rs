mod chunk;
#[macro_use]
mod macros;
mod compiler;
mod object;
mod op_code;
mod scanner;
mod value;
mod vm;

use crate::{compiler::compile, vm::Vm};
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
    if let Some(chunk) = compile(&source) {
        let mut vm = Vm::new();
        let result = vm.interpret_chunk(chunk);
        println!("{result:?}");
    }
}

fn repl() {
    let mut stdout = stdout();
    print!("> ");
    stdout.flush().unwrap();

    let mut vm = Vm::new();

    for line in stdin().lines() {
        let line = line.unwrap();
        match compile(&line) {
            Some(chunk) => {
                let result = vm.interpret_chunk(chunk);
                println!("{result:?}");
            }
            None => {
                println!("Could not compile");
            }
        }

        print!("> ");
        stdout.flush().unwrap();
    }
}
