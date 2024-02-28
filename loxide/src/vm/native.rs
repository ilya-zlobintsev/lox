use std::time::SystemTime;

use crate::value::Value;

pub fn clock(_args: &[Value]) -> Value {
    let timestamp = SystemTime::now()
        .duration_since(SystemTime::UNIX_EPOCH)
        .unwrap()
        .as_millis();
    Value::Number(timestamp as f64)
}
