mod chunk;
mod op_code;
mod value;
mod vm;

use crate::chunk::Chunk;
use op_code::OpCode;
use vm::Vm;

fn main() {
    let mut chunk = Chunk::default();

    let constant = chunk.add_constant(2.0);
    chunk.write(OpCode::Constant, 123);
    chunk.write(constant as u8, 123);

    let constant = chunk.add_constant(3.0);
    chunk.write(OpCode::Constant, 123);
    chunk.write(constant as u8, 123);

    chunk.write(OpCode::Multiply, 123);

    let constant = chunk.add_constant(1.0);
    chunk.write(OpCode::Constant, 123);
    chunk.write(constant as u8, 123);

    chunk.write(OpCode::Add, 123);

    chunk.write(OpCode::Return, 123);

    chunk.disassemble("test chunk");

    let result = Vm::interpret(chunk);
    println!("{result:?}");
}
