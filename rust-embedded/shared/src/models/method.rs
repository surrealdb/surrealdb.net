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
    Upsert = 9,
    Merge = 10,
    Patch = 11,
    Delete = 12,
    Version = 13,
    Query = 14,
    Relate = 15,
    Run = 16,
    InsertRelation = 17,
    Sessions = 18,
    Attach = 19,
    Detach = 20,
}

impl From<Method> for surrealdb::rpc::Method {
    fn from(value: Method) -> Self {
        match value {
            Method::Ping => surrealdb::rpc::Method::Ping,
            Method::Use => surrealdb::rpc::Method::Use,
            Method::Set => surrealdb::rpc::Method::Set,
            Method::Unset => surrealdb::rpc::Method::Unset,
            Method::Select => surrealdb::rpc::Method::Select,
            Method::Insert => surrealdb::rpc::Method::Insert,
            Method::Create => surrealdb::rpc::Method::Create,
            Method::Update => surrealdb::rpc::Method::Update,
            Method::Upsert => surrealdb::rpc::Method::Upsert,
            Method::Merge => surrealdb::rpc::Method::Merge,
            Method::Patch => surrealdb::rpc::Method::Patch,
            Method::Delete => surrealdb::rpc::Method::Delete,
            Method::Version => surrealdb::rpc::Method::Version,
            Method::Query => surrealdb::rpc::Method::Query,
            Method::Relate => surrealdb::rpc::Method::Relate,
            Method::Run => surrealdb::rpc::Method::Run,
            Method::InsertRelation => surrealdb::rpc::Method::InsertRelation,
            Method::Sessions => surrealdb::rpc::Method::Sessions,
            Method::Attach => surrealdb::rpc::Method::Attach,
            Method::Detach => surrealdb::rpc::Method::Detach,
        }
    }
}
