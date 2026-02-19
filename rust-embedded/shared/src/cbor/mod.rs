use anyhow::anyhow;
use surrealdb::rpc::format::cbor::decode;
use surrealdb_types::{Array, Value};

pub fn get_params(val: Vec<u8>) -> anyhow::Result<Array> {
    let value = decode(val.as_slice()).map_err(|_| anyhow!("Parameters are not valid CBOR."))?;

    if let Value::Array(arr) = value {
        Ok(arr)
    } else {
        Err(anyhow!("Parameters are not a valid Array."))
    }
}
