use crate::{op_code::OpCode, value::Value};

#[derive(Default, Debug)]
pub struct Chunk {
    pub code: Vec<u8>,
    // Simple run-length encoding
    lines: Vec<LineInfo>,
    pub constants: Vec<Value>,
}

#[derive(Default, Debug)]
struct LineInfo {
    start_offset: usize,
    line: u32,
}

impl Chunk {
    pub fn write(&mut self, data: impl Into<u8>, line: u32) {
        self.code.push(data.into());

        if !self
            .lines
            .last()
            .is_some_and(|last_line| last_line.line == line)
        {
            self.lines.push(LineInfo {
                start_offset: self.code.len() - 1,
                line,
            });
        }
    }

    pub fn write_slice(&mut self, data: &[u8], line: u32) {
        self.code.reserve(data.len());

        for byte in data {
            self.write(*byte, line);
        }
    }

    pub fn disassemble(&self, name: &str) {
        println!("== {name} ==");

        // let mut code_iter = self.code.iter().enumerate();

        let mut offset = 0;
        while offset < self.code.len() {
            offset = self.disassemble_instruction(offset);
        }
    }

    pub fn disassemble_instruction(&self, mut offset: usize) -> usize {
        let code = self.code[offset];

        print!("{offset:04} ");

        if offset > 0 && self.line_at(offset) == self.line_at(offset - 1) {
            print!("   | ");
        } else {
            print!("{:4} ", self.line_at(offset));
        }

        let op_code = OpCode::from_byte(code).unwrap();
        let name = format!("{op_code}");
        match op_code {
            OpCode::Constant => {
                offset += 1;

                let index = self.code[offset];
                let value = &self.constants[index as usize];
                println!("{name:<16} {index} '{value:?}'");
            }
            OpCode::LongConstant => {
                let mut index_data = [0; 4];
                index_data[0..3].copy_from_slice(&self.code[offset + 1..offset + 4]);

                let index = u32::from_le_bytes(index_data);
                let value = self.constants[index as usize];

                println!("{name:<16} {index} '{value:?}'");

                offset += 3;
                // let mut index_data = [0; 4];
                // for i in 1..=3 {
                //     code_iter.next()
                // }
            }
            _ => println!("{name}"),
        }
        offset += 1;
        offset
    }

    pub fn add_constant(&mut self, value: Value) -> usize {
        self.constants.push(value);
        self.constants.len() - 1
    }

    fn line_at(&self, offset: usize) -> u32 {
        for (i, info) in self.lines.iter().enumerate() {
            if info.start_offset > offset {
                return self.lines[i - 1].line;
            }
        }
        self.lines.last().unwrap().line
    }
}

#[cfg(test)]
mod tests {
    use super::Chunk;
    use crate::op_code::OpCode;

    #[test]
    fn lines() {
        let mut chunk = Chunk::default();
        chunk.write(OpCode::Return, 2); // offset 0
        chunk.write(OpCode::Constant, 3); // offset 1
        chunk.write(1, 3); // offset 2
        chunk.write(OpCode::Return, 5); // offset 3

        println!("{chunk:?}");

        assert_eq!(2, chunk.line_at(0));
        assert_eq!(3, chunk.line_at(1));
        assert_eq!(3, chunk.line_at(2));
        assert_eq!(5, chunk.line_at(3));
    }
}
