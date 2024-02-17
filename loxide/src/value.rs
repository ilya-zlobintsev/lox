#[derive(Debug, PartialEq, Clone, Copy)]
pub enum Value {
    Number(f64),
    Boolean(bool),
    Nil,
}

use Value::*;
impl Value {
    pub fn as_bool(&self) -> Option<bool> {
        match self {
            Boolean(value) => Some(*value),
            _ => None,
        }
    }

    pub fn as_bool_mut(&mut self) -> Option<&mut bool> {
        match self {
            Boolean(value) => Some(value),
            _ => None,
        }
    }

    pub fn as_number(&self) -> Option<f64> {
        match self {
            Number(value) => Some(*value),
            _ => None,
        }
    }

    pub fn as_number_mut(&mut self) -> Option<&mut f64> {
        match self {
            Number(value) => Some(value),
            _ => None,
        }
    }

    pub fn is_falsey(&self) -> bool {
        match self {
            Boolean(value) => !value,
            Nil => true,
            _ => false,
        }
    }
}

macro_rules! impl_conversions {
    ($($variant:ident, $type:ty,)+) => {
        $(

        impl From<$type> for Value {
            fn from(value: $type) -> Self {
                Self::$variant(value)
            }
        }

        impl TryFrom<Value> for $type {
            type Error = &'static str;

            fn try_from(value: Value) -> Result<Self, Self::Error> {
                match value {
                    Value::$variant(value) => Ok(value),
                    _ => Err(concat!("Value is not a ", stringify!($variant))),
                }
            }
        }
        )+
    };
}

impl_conversions!(Boolean, bool, Number, f64,);
