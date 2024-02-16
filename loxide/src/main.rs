mod chunk;
mod compiler;
mod op_code;
mod scanner;
mod value;
mod vm;

use std::{
    env, fs,
    io::{stdin, stdout, Stdout, Write},
};

use crate::chunk::Chunk;
use op_code::OpCode;
use vm::{InterpretResult, Vm};

fn main() {
    let mut args = env::args().skip(1);

    if let Some(file_path) = args.next() {
        run_file(&file_path)
    } else {
        repl()
    }

    // let mut chunk = Chunk::default();

    // let constant = chunk.add_constant(2.0);
    // chunk.write(OpCode::Constant, 123);
    // chunk.write(constant as u8, 123);

    // let constant = chunk.add_constant(3.0);
    // chunk.write(OpCode::Constant, 123);
    // chunk.write(constant as u8, 123);

    // chunk.write(OpCode::Multiply, 123);

    // let constant = chunk.add_constant(1.0);
    // chunk.write(OpCode::Constant, 123);
    // chunk.write(constant as u8, 123);

    // chunk.write(OpCode::Add, 123);

    // chunk.write(OpCode::Return, 123);

    // chunk.disassemble("test chunk");

    // let result = Vm::interpret(chunk);
    // println!("{result:?}");
}

fn run_file(path: &str) {
    let source = fs::read_to_string(path).unwrap();

    todo!()
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
