use std::{collections::BTreeMap, sync::Arc};
use surrealdb::{
    engine::local::Db,
    sql::{Array, Value},
    Surreal,
};
use surrealdb_core::rpc::args::Take;

use crate::bindgen::callback::{send_failure, send_success, FailureAction, SuccessAction};

pub async fn select_async(
    client: Arc<Surreal<Db>>,
    params: Array,
    success: SuccessAction,
    failure: FailureAction,
) {
    let Ok(what) = params.needs_one() else {
        send_failure("Invalid params", failure);
        return;
    };

    let one = what.is_thing();

    let sql = "SELECT * FROM $what";

    let mut vars: BTreeMap<&str, Value> = BTreeMap::new();
    vars.insert("what", what.could_be_table());

    let response = client.query(sql).bind(vars).await;

    match response {
        Ok(mut response) => {
            let result = response.take::<Value>(0);

            match result {
                Ok(value) => {
                    let value = match one {
                        true => match value {
                            Value::Array(v) => {
                                if v.is_empty() {
                                    Value::None
                                } else {
                                    v.first().unwrap_or(&Value::None).clone()
                                }
                            }
                            _ => value,
                        },
                        false => value,
                    };

                    send_success(value, success, failure);
                }
                Err(error) => {
                    send_failure(error.to_string().as_str(), failure);
                }
            }
        }
        Err(error) => {
            send_failure(error.to_string().as_str(), failure);
        }
    }
}
