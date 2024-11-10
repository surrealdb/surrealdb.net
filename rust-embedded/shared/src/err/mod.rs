#[derive(Debug)]
pub struct Error(String);

impl Error {
    pub fn as_str(&self) -> &str {
        &self.0
    }
}

impl From<surrealdb::err::Error> for Error {
    fn from(v: surrealdb::err::Error) -> Self {
        Self(v.to_string())
    }
}

impl From<&str> for Error {
    fn from(v: &str) -> Self {
        Self(v.to_string())
    }
}

impl From<String> for Error {
    fn from(v: String) -> Self {
        Self(v)
    }
}
