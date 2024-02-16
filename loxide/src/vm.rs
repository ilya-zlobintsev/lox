use crate::{chunk::Chunk, compiler, op_code::OpCode, scanner::Scanner, value::Value};

pub struct Vm {
    chunk: Chunk,
    ip: usize,
    stack: Vec<Value>,
}

impl Vm {
    pub fn interpret(source: &str) -> InterpretResult {
        compiler::compile(source);

        InterpretResult::Ok(None)
    }

    pub fn interpret_chunk(chunk: Chunk) -> InterpretResult {
        let vm = Self {
            chunk,
            ip: 0,
            stack: Vec::new(),
        };
        vm.run()
    }

    fn run(mut self) -> InterpretResult {
        loop {
            #[cfg(feature = "trace")]
            {
                print!("          ");
                for slot in 0..self.stack.len() {
                    print!("[ {} ]", self.stack[slot]);
                }
                println!();

                self.chunk.disassemble_instruction(self.ip);
            }

            let byte = self.read_byte();
            let op_code = OpCode::from_byte(byte).expect("Read invalid opcode");

            use OpCode::*;
            match op_code {
                Return => {
                    break InterpretResult::Ok(self.stack.pop());
                }
                Constant => {
                    let value = self.read_constant();
                    self.stack.push(value);
                }
                LongConstant => {
                    let value = self.read_long_constant();
                    self.stack.push(value);
                }
                Negate => {
                    *self.stack.last_mut().unwrap() *= -1.0;
                }
                Add => self.binary_op(|a, b| a + b),
                Subtract => self.binary_op(|a, b| a - b),
                Multiply => self.binary_op(|a, b| a * b),
                Divide => self.binary_op(|a, b| a / b),
            }
        }
    }

    fn read_byte(&mut self) -> u8 {
        let byte = self.chunk.code[self.ip];
        self.ip += 1;
        byte
    }

    fn read_multi<const LEN: usize>(&mut self) -> &[u8] {
        let data = &self.chunk.code[self.ip..self.ip + LEN];
        self.ip += LEN;
        data
    }

    fn read_constant(&mut self) -> Value {
        let index = self.read_byte();
        self.chunk.constants[index as usize]
    }

    fn read_long_constant(&mut self) -> Value {
        let data = self.read_multi::<3>();
        let mut index_data = [0; 4];
        index_data[0..3].copy_from_slice(data);

        let index = u32::from_le_bytes(index_data);
        self.chunk.constants[index as usize]
    }

    // fn reset_stack(&mut self) {
    //     self.stack_top = 0;
    // }

    fn binary_op<Op: FnOnce(Value, Value) -> Value>(&mut self, op: Op) {
        let b = self.stack.pop().unwrap();
        let a = self.stack.pop().unwrap();
        self.stack.push(op(a, b));
    }
}

#[derive(Debug, PartialEq)]
pub enum InterpretResult {
    Ok(Option<Value>),
    CompileError,
    RuntimeError,
}

#[cfg(test)]
mod tests {
    use super::Vm;
    use crate::{chunk::Chunk, op_code::OpCode, vm::InterpretResult};

    #[test]
    fn basic_math() {
        let mut chunk = Chunk::default();

        let constant = chunk.add_constant(1.2);
        chunk.write(OpCode::Constant, 123);
        chunk.write(constant as u8, 123);

        let constant = chunk.add_constant(3.4);
        chunk.write(OpCode::Constant, 123);
        chunk.write(constant as u8, 123);

        chunk.write(OpCode::Add, 123);

        let constant = chunk.add_constant(5.6);
        chunk.write(OpCode::Constant, 123);
        chunk.write(constant as u8, 123);

        chunk.write(OpCode::Divide, 123);

        chunk.write(OpCode::Negate, 123);
        chunk.write(OpCode::Return, 123);

        let result = Vm::interpret_chunk(chunk);
        assert_eq!(InterpretResult::Ok(Some(-0.8214285714285714)), result);
    }

    #[test]
    fn add_long_constants() {
        let mut chunk = Chunk::default();

        let constant_long = chunk.add_constant(42.0);
        chunk.write(OpCode::LongConstant, 123);
        chunk.write_slice(&constant_long.to_le_bytes()[0..3], 123);

        let constant_long = chunk.add_constant(3.0);
        chunk.write(OpCode::LongConstant, 123);
        chunk.write_slice(&constant_long.to_le_bytes()[0..3], 123);

        chunk.write(OpCode::Add, 123);
        chunk.write(OpCode::Return, 123);

        let result = Vm::interpret_chunk(chunk);
        assert_eq!(InterpretResult::Ok(Some(45.0)), result);
    }
}
