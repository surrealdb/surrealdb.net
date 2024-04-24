#[repr(u8)]
pub enum Method {
    Connect = 1,
    Ping = 2,
    Use = 3,
    Set = 4,
    Unset = 5,
    Select = 6,
    //Insert = 7, // TODO
    Create = 8,
    Update = 9,
    Merge = 10,
    Patch = 11,
    Delete = 12,
    Version = 13,
    Query = 14,
    // TODO : Relate
}
