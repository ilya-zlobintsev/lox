convertable_enum! {
    OpCode,
    Return = 0,
    Constant = 1,
    LongConstant = 2,
    Negate = 3,
    Add = 4,
    Subtract = 5,
    Multiply = 6,
    Divide = 7,
    Nil = 8,
    True = 9,
    False = 10,
    Not = 11,
    Equal = 12,
    Greater = 13,
    Less = 14,
    Print = 15,
    Pop = 16,
    DefineGlobal = 17,
    GetGlobal = 18,
    SetGlobal = 19,
}