#[repr(u8)]
pub enum Method {
    Ping = 1,
    Use = 2,
    Set = 3,
    Unset = 4,
    Select = 5,
    Insert = 6,
    Create = 7,
    Update = 8,
    Merge = 9,
    Patch = 10,
    Delete = 11,
    Version = 12,
    Query = 13,
    Relate = 14,
}

impl From<Method> for surrealdb::rpc::method::Method {
    fn from(value: Method) -> Self {
        match value {
            Method::Ping => surrealdb::rpc::method::Method::Ping,
            Method::Use => surrealdb::rpc::method::Method::Use,
            Method::Set => surrealdb::rpc::method::Method::Set,
            Method::Unset => surrealdb::rpc::method::Method::Unset,
            Method::Select => surrealdb::rpc::method::Method::Select,
            Method::Insert => surrealdb::rpc::method::Method::Insert,
            Method::Create => surrealdb::rpc::method::Method::Create,
            Method::Update => surrealdb::rpc::method::Method::Update,
            Method::Merge => surrealdb::rpc::method::Method::Merge,
            Method::Patch => surrealdb::rpc::method::Method::Patch,
            Method::Delete => surrealdb::rpc::method::Method::Delete,
            Method::Version => surrealdb::rpc::method::Method::Version,
            Method::Query => surrealdb::rpc::method::Method::Query,
            Method::Relate => surrealdb::rpc::method::Method::Relate,
        }
    }
}
