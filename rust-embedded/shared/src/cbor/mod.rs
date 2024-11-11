use ciborium::Value as Data;
use surrealdb::rpc::format::cbor::Cbor;
use surrealdb::sql::{Array, Value};

use crate::app::Error;

pub fn get_params(val: Vec<u8>) -> Result<Array, Error> {
    let data = ciborium::from_reader::<Data, _>(&mut val.as_slice())
        .map_err(|_| Error::from("Parameters are not valid CBOR."));
    let data = data.map(Cbor)?;

    let value: Value = data
        .try_into()
        .map_err(|_| Error::from("Failed to convert CBOR into Value."))?;

    if let Value::Array(arr) = value {
        Ok(arr)
    } else {
        Err(Error::from("Parameters are not a valid Array."))
    }
}
