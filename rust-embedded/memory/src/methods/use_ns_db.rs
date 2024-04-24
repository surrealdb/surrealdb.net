use std::sync::Arc;
use surrealdb::{
    engine::local::Db,
    sql::{Array, Value},
    Surreal,
};
use surrealdb_core::rpc::args::Take;

use crate::bindgen::callback::{send_failure, send_success, FailureAction, SuccessAction};

pub async fn use_ns_db_async(
    client: Arc<Surreal<Db>>,
    params: Array,
    success: SuccessAction,
    failure: FailureAction,
) {
    let (ns, db) = match params.needs_two() {
        Ok((Value::Strand(ns), Value::Strand(db))) => (ns.0, db.0),
        _ => {
            send_failure("Invalid parameters", failure);
            return;
        }
    };

    let result = client.use_ns(ns).use_db(db).await;

    match result {
        Ok(_) => {
            send_success(Value::None, success, failure);
        }
        Err(error) => {
            send_failure(error.to_string().as_str(), failure);
        }
    }
}
