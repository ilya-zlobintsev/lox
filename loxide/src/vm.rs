use crate::{chunk::Chunk, compiler, object::Object, op_code::OpCode, value::Value};

pub struct Vm {
    chunk: Chunk,
    ip: usize,
    stack: Vec<Value>,
}

impl Vm {
    pub fn interpret(source: &str) -> InterpretResult {
        match compiler::compile(source) {
            Some(chunk) => Self::interpret_chunk(chunk),
            None => Err(VmError::CompileError),
        }
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
                    print!("[ {:?} ]", self.stack[slot]);
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
                    let value = self.load_constant();
                    self.stack.push(value);
                }
                LongConstant => {
                    let value = self.load_long_constant();
                    self.stack.push(value);
                }
                Negate => match self.peek_mut(0) {
                    Value::Number(value) => *value *= -1.0,
                    Value::Object(Object::String(str)) => {
                        let reversed: String = str.chars().rev().collect();
                        *str = reversed.into();
                    }
                    _ => self.runtime_error("Operand must be a number or a string")?,
                },
                Add => match (self.peek(0), self.peek(1)) {
                    (Value::Object(Object::String(_)), Value::Object(Object::String(_))) => {
                        let b = self.stack.pop().unwrap();
                        let a = self.stack.pop().unwrap();
                        let new_value = format!("{}{}", a.as_str().unwrap(), b.as_str().unwrap());
                        self.stack.push(Value::new_string(new_value));
                    }
                    _ => self.binary_op(|a, b| a + b)?,
                },
                Subtract => self.binary_op(|a, b| a - b)?,
                Multiply => self.binary_op(|a, b| a * b)?,
                Divide => self.binary_op(|a, b| a / b)?,
                Greater => self.binary_op(|a, b| a > b)?,
                Less => self.binary_op(|a, b| a < b)?,
                Equal => {
                    let b = self.stack.pop().unwrap();
                    let a = self.stack.pop().unwrap();
                    self.stack.push((a == b).into());
                }
                Nil => self.stack.push(Value::Nil),
                True => self.stack.push(true.into()),
                False => self.stack.push(false.into()),
                Not => {
                    let value = self.stack.pop().unwrap();
                    self.stack.push(value.is_falsey().into())
                }
            }
        }
    }

    fn runtime_error(&self, message: &str) -> Result<(), VmError> {
        eprintln!(
            "[line {}] Error in script: {message}",
            self.chunk.line_at(self.ip)
        );
        Err(VmError::RuntimeError)
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

    fn load_constant(&mut self) -> Value {
        let index = self.read_byte();
        self.chunk.constants[index as usize].clone()
    }

    fn load_long_constant(&mut self) -> Value {
        let data = self.read_multi::<3>();
        let mut index_data = [0; 4];
        index_data[0..3].copy_from_slice(data);

        let index = u32::from_le_bytes(index_data);
        self.chunk.constants[index as usize].clone()
    }

    fn binary_op<V, Op>(&mut self, op: Op) -> Result<(), VmError>
    where
        V: Into<Value>,
        Op: FnOnce(f64, f64) -> V,
    {
        let b = self.stack.pop().unwrap();
        let a = self.stack.pop().unwrap();

        match (a, b) {
            (Value::Number(lhs), Value::Number(rhs)) => {
                let result = op(lhs, rhs);
                self.stack.push(result.into());
                Ok(())
            }
            _ => self.runtime_error("Operands have invalid types"),
        }
    }

    fn peek(&self, distance: usize) -> &Value {
        &self.stack[self.stack.len() - 1 - distance]
    }

    fn peek_mut(&mut self, distance: usize) -> &mut Value {
        let index = self.stack.len() - 1 - distance;
        &mut self.stack[index]
    }
}

pub type InterpretResult = Result<Option<Value>, VmError>;

#[derive(Debug, PartialEq)]
pub enum VmError {
    CompileError,
    RuntimeError,
}

#[cfg(test)]
mod tests {
    use super::Vm;
    use crate::{chunk::Chunk, op_code::OpCode, value::Value, vm::InterpretResult};

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
        assert_eq!(
            InterpretResult::Ok(Some(Value::Number(-0.821_428_571_428_571_4))),
            result
        );
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
        assert_eq!(InterpretResult::Ok(Some(Value::Number(45.0))), result);
    }
}
