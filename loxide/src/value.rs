use std::{
    fmt::{self},
    rc::Rc,
};

use crate::object::Object;

#[derive(Debug, PartialEq, Clone)]
pub enum Value {
    Number(f64),
    Boolean(bool),
    Object(Object),
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

    pub fn as_str(&self) -> Option<&str> {
        if let Self::Object(Object::String(str)) = self {
            Some(str)
        } else {
            None
        }
    }

    pub fn new_string(value: impl Into<Rc<str>>) -> Self {
        Self::Object(Object::String(value.into()))
    }
}

impl fmt::Display for Value {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        match self {
            Number(num) => num.fmt(f),
            Boolean(bool) => bool.fmt(f),
            Object(obj) => obj.fmt(f),
            Nil => f.write_str("nil"),
        }
    }
}

impl_enum_conversions! {
    Value,
    Boolean, bool,
    Number, f64,
    Object, Object,
}
