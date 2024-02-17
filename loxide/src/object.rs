use std::rc::Rc;

#[derive(Debug, PartialEq, Clone)]
pub enum Object {
    String(Rc<str>),
}

impl_enum_conversions! {
    Object,
    String, Rc<str>,
}

impl From<&str> for Object {
    fn from(value: &str) -> Self {
        Object::String(value.into())
    }
}
