use std::fmt::{self, Display};

#[derive(Debug, Clone, Copy)]
pub enum OpCode {
    Return,
    Constant,
    LongConstant,
}

impl OpCode {
    pub fn from_byte(byte: u8) -> Option<Self> {
        use OpCode::*;
        let op_code = match byte {
            0 => Return,
            1 => Constant,
            2 => LongConstant,
            _ => return None,
        };
        Some(op_code)
    }

    pub fn as_byte(&self) -> u8 {
        use OpCode::*;
        match self {
            Return => 0,
            Constant => 1,
            LongConstant => 2,
        }
    }
}

impl From<OpCode> for u8 {
    fn from(value: OpCode) -> Self {
        value.as_byte()
    }
}

impl Display for OpCode {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        fmt::Debug::fmt(self, f)
    }
}
