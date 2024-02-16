use crate::{chunk::Chunk, op_code::OpCode, value::Value};

pub struct Vm {
    chunk: Chunk,
    ip: usize,
}

impl Vm {
    pub fn interpret(chunk: Chunk) -> InterpretResult {
        let vm = Self { chunk, ip: 0 };
        vm.run()
    }

    fn run(mut self) -> InterpretResult {
        loop {
            #[cfg(feature = "trace")]
            self.chunk.disassemble_instruction(self.ip);

            let byte = self.read_byte();
            let op_code = OpCode::from_byte(byte).expect("Read invalid opcode");

            match op_code {
                OpCode::Return => break InterpretResult::Ok,
                OpCode::Constant => {
                    let value = self.read_constant();
                    println!("{value}");
                }
                OpCode::LongConstant => {
                    let value = self.read_long_constant();
                    println!("{value}");
                }
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
}

pub enum InterpretResult {
    Ok,
    CompileError,
    RuntimeError,
}
