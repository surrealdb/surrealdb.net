use anyhow::anyhow;
use surrealdb_types::{Array, Value};

/// The nesting depth a decoded CBOR payload may reach.
///
/// `surrealdb-core` 3.3 requires an explicit limit. 256 is the limit `ciborium::from_reader`
/// applied implicitly before 3.3, so payloads are accepted or rejected exactly as they were.
const CBOR_RECURSION_LIMIT: usize = 256;

pub fn decode(bytes: &[u8]) -> anyhow::Result<Value> {
    surrealdb::rpc::format::cbor::decode(bytes, CBOR_RECURSION_LIMIT)
}

pub fn get_params(val: Vec<u8>) -> anyhow::Result<Array> {
    let value = decode(val.as_slice()).map_err(|_| anyhow!("Parameters are not valid CBOR."))?;

    if let Value::Array(arr) = value {
        Ok(arr)
    } else {
        Err(anyhow!("Parameters are not a valid Array."))
    }
}
