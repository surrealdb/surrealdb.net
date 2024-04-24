use std::sync::Arc;
use surrealdb::{
    engine::local::Db,
    sql::{Array, Value},
    Surreal,
};
use surrealdb_core::rpc::args::Take;

use crate::bindgen::callback::{send_failure, send_success, FailureAction, SuccessAction};

pub async fn unset_async(
    client: Arc<Surreal<Db>>,
    params: Array,
    success: SuccessAction,
    failure: FailureAction,
) {
    let Ok(Value::Strand(key)) = params.needs_one() else {
        send_failure("Invalid params", failure);
        return;
    };

    let response = client.unset(key.0).await;

    match response {
        Ok(_) => {
            send_success(Value::None, success, failure);
        }
        Err(error) => {
            send_failure(error.to_string().as_str(), failure);
        }
    }
}
