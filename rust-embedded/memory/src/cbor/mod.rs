use ciborium::Value as Data;
use surrealdb::rpc::format::cbor::Cbor;
use surrealdb::sql::{Array, Value};

pub fn get_params(val: Vec<u8>) -> Result<Array, ()> {
    let data = ciborium::from_reader::<Data, _>(&mut val.as_slice()).map_err(|_| ());
    let data = data.map(Cbor)?;

    let value: Value = data.try_into().map_err(|_| ())?;

    if let Value::Array(arr) = value {
        Ok(arr)
    } else {
        Err(())
    }
}
