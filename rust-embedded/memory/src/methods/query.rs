use std::{collections::BTreeMap, sync::Arc};
use surrealdb::{
    engine::local::Db,
    sql::{Array, Duration, Object, Strand, Value},
    Surreal,
};
use surrealdb_core::rpc::args::Take;

use crate::bindgen::callback::{send_failure, send_success, FailureAction, SuccessAction};

pub async fn query_async(
    client: Arc<Surreal<Db>>,
    params: Array,
    success: SuccessAction,
    failure: FailureAction,
) {
    let Ok((query, o)) = params.needs_one_or_two() else {
        send_failure("Invalid params", failure);
        return;
    };

    let query = match query {
        Value::Strand(v) => v.as_string(),
        _ => {
            send_failure("Invalid params", failure);
            return;
        }
    };

    let o = match o {
        Value::Object(v) => Some(v),
        Value::None | Value::Null => None,
        _ => {
            send_failure("Invalid params", failure);
            return;
        }
    };

    let vars = match o {
        Some(v) => Some(v.0),
        None => None,
    };

    let response = client.query(&query).bind(vars).with_stats().await;

    match response {
        Ok(mut response) => {
            let count = response.num_statements();

            let mut array = Array::with_capacity(count);

            for index in 0..count {
                let result = response.take::<Value>(index);

                match result {
                    Some(result) => {
                        let (stats, result) = result;
                        let mut o: BTreeMap<&str, Value> = BTreeMap::new();

                        let time = stats
                            .execution_time
                            .map(|d| Duration::from(d).to_raw())
                            .unwrap_or("0ns".to_string());
                        o.insert("time", Value::Strand(Strand::from(time)));

                        match result {
                            Ok(v) => {
                                o.insert("status", Value::Strand(Strand::from("OK")));
                                o.insert("result", v);
                            }
                            Err(e) => {
                                o.insert("status", Value::Strand(Strand::from("ERR")));
                                o.insert(
                                    "errorDetails",
                                    Value::Strand(Strand::from(e.to_string())),
                                );
                            }
                        }

                        array.push(Value::Object(Object::from(o)));
                    }
                    None => {
                        array.push(Value::None);
                    }
                }
            }

            let value = Value::Array(array);

            send_success(value, success, failure);
        }
        Err(error) => {
            send_failure(error.to_string().as_str(), failure);
        }
    }
}
