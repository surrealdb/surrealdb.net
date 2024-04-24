use std::sync::Arc;
use surrealdb::{engine::local::Db, sql::Value, Surreal};

use crate::bindgen::callback::{send_failure, send_success, FailureAction, SuccessAction};

pub async fn ping_async(client: Arc<Surreal<Db>>, success: SuccessAction, failure: FailureAction) {
    let result = client.health().await;

    match result {
        Ok(_) => {
            send_success(Value::None, success, failure);
        }
        Err(error) => {
            send_failure(error.to_string().as_str(), failure);
        }
    }
}
