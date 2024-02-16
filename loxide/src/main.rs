mod chunk;
mod op_code;
mod value;
mod vm;

use crate::chunk::Chunk;
use op_code::OpCode;
use vm::Vm;

fn main() {
    let mut chunk = Chunk::default();

    let constant = chunk.add_constant(1.2);
    chunk.write(OpCode::Constant, 123);
    chunk.write(constant as u8, 123);

    let constant_long = chunk.add_constant(42.0);
    chunk.write(OpCode::LongConstant, 123);
    chunk.write_slice(&constant_long.to_le_bytes()[0..3], 123);

    chunk.write(OpCode::Return, 123);

    chunk.disassemble("test chunk");

    println!("Interpreting");
    Vm::interpret(chunk);
}
