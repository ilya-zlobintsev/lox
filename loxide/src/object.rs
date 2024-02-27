use crate::chunk::Chunk;
use std::{fmt, rc::Rc};

#[derive(Debug, PartialEq, Clone)]
pub enum Object {
    String(Rc<str>),
    Function(FunctionObject),
}

#[derive(PartialEq, Clone)]
pub struct FunctionObject {
    pub arity: u8,
    pub chunk: Chunk,
    pub name: Rc<str>,
}

impl Default for FunctionObject {
    fn default() -> Self {
        Self {
            arity: Default::default(),
            chunk: Default::default(),
            name: "<placeholder>".into(),
        }
    }
}

impl fmt::Debug for FunctionObject {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        f.debug_struct("FunctionObject")
            .field("name", &self.name)
            .field("arity", &self.arity)
            .finish()
    }
}

impl_enum_conversions! {
    Object,
    String, Rc<str>,
    Function, FunctionObject,
}

impl From<&str> for Object {
    fn from(value: &str) -> Self {
        Object::String(value.into())
    }
}
