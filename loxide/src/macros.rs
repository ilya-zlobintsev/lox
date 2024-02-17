#[macro_export]
macro_rules! convertable_enum {
    ($enum:ident, $($name:ident = $value:literal,)+) => {
        #[derive(Debug, Clone, Copy)]
        pub enum $enum {
            $(
                $name = $value,
            )+
        }

        impl $enum {
            pub fn as_byte(&self) -> u8 {
                *self as u8
            }

            pub fn from_byte(byte: u8) -> Option<Self> {
                match byte {
                    $(
                        $value => Some($enum::$name),
                    )+
                    _ => None,
                }
            }
        }

        impl From<$enum> for u8 {
            fn from(value: $enum) -> Self {
                value.as_byte()
            }
        }

        use std::fmt;
        impl fmt::Display for $enum {
            fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
                fmt::Debug::fmt(self, f)
            }
        }

        impl PartialEq for $enum {
            fn eq(&self, other: &Self) -> bool {
                self.as_byte() == other.as_byte()
            }
        }

        impl Eq for $enum {}

        use std::cmp;
        impl PartialOrd for $enum {
            fn partial_cmp(&self, other: &Self) -> Option<cmp::Ordering> {
                Some(self.as_byte().cmp(&other.as_byte()))
            }
        }

        impl Ord for $enum {
            fn cmp(&self, other: &Self) -> cmp::Ordering {
                self.as_byte().cmp(&other.as_byte())
            }
        }
    };
}

macro_rules! impl_enum_conversions {
    ($enum:ident, $($variant:ident, $type:ty,)+) => {
        $(
        impl From<$type> for $enum {
            fn from(value: $type) -> Self {
                Self::$variant(value)
            }
        }

        impl TryFrom<$enum> for $type {
            type Error = &'static str;

            fn try_from(value: $enum) -> Result<Self, Self::Error> {
                #[allow(unreachable_patterns)]
                match value {
                    $enum::$variant(value) => Ok(value),
                    _ => Err(concat!("Value is not a ", stringify!($variant))),
                }
            }
        }
        )+
    };
}
