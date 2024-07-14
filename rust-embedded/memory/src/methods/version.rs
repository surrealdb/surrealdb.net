use surrealdb::sql::{Strand, Value};

use crate::bindgen::callback::{send_success, FailureAction, SuccessAction};

pub async fn version_async(success: SuccessAction, failure: FailureAction) {
    static VERSION: &str = include_str!("../surreal-version.txt");

    let value = Value::Strand(Strand::from(VERSION));
    send_success(value, success, failure);
}
