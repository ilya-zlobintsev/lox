use std::fmt::{self, Display};

macro_rules! instructions {
    ($($name:ident = $value:literal,)+) => {
        #[derive(Debug, Clone, Copy)]
        pub enum OpCode {
            $(
                $name,
            )+
        }

        impl OpCode {
            pub fn as_byte(&self) -> u8 {
                match self {
                    $(
                        OpCode::$name => $value,
                    )+
                }
            }

            pub fn from_byte(byte: u8) -> Option<Self> {
                match byte {
                    $(
                        $value => Some(OpCode::$name),
                    )+
                    _ => None,
                }
            }
        }
    };
}

instructions! {
    Return = 0,
    Constant = 1,
    LongConstant = 2,
    Negate = 3,
    Add = 4,
    Subtract = 5,
    Multiply = 6,
    Divide = 7,
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
