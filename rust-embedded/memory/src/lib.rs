use app::SurrealEmbeddedEngine;
use bindgen::{
    callback::{send_failure, send_success, FailureAction, SuccessAction},
    csharp_to_rust::convert_csharp_to_rust_bytes,
};
use models::method::Method;
use runtime::{engines::ENGINES, get_global_runtime};

mod app;
mod bindgen;
mod cbor;
mod err;
mod models;
mod runtime;

// 💡 "connect" is a reserved keyword
#[no_mangle]
pub extern "C" fn apply_connect(id: i32, success: SuccessAction, failure: FailureAction) {
    get_global_runtime().spawn(async move {
        let Ok(engine) = SurrealEmbeddedEngine::connect("memory".to_string()).await else {
            send_failure("Cannot connect to db", failure);
            return;
        };
        ENGINES.insert(id, engine).await;
        send_success(vec![], success);
    });
}

#[no_mangle]
pub extern "C" fn execute(
    id: i32,
    method: Method,
    bytes: *const u8,
    len: i32,
    success: SuccessAction,
    failure: FailureAction,
) {
    let method: surrealdb::rpc::method::Method = method.into();
    let params_bytes = convert_csharp_to_rust_bytes(bytes, len);

    get_global_runtime().spawn(async move {
        match ENGINES.execute(id, method, params_bytes).await {
            Ok(output) => {
                send_success(output, success);
            }
            Err(error) => {
                send_failure(error.as_str(), failure);
            }
        }
    });
}
