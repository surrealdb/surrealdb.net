use app::SurrealEmbeddedEngine;
use bindgen::{
    callback::{send_failure, send_success, FailureAction, SuccessAction},
    csharp_to_rust::{convert_csharp_to_rust_bytes, convert_csharp_to_rust_string_utf16},
};
use models::method::Method;
use runtime::{engines::ENGINES, get_global_runtime};

pub mod app;
pub mod bindgen;
pub mod cbor;
pub mod err;
pub mod models;
pub mod runtime;

// 💡 "connect" is a reserved keyword
#[no_mangle]
pub extern "C" fn apply_connect(
    id: i32, 
    utf16_str: *const u16,
    utf16_len: i32,
    success: SuccessAction, 
    failure: FailureAction
) {
    let endpoint = convert_csharp_to_rust_string_utf16(utf16_str, utf16_len);

    get_global_runtime().spawn(async move {
        match SurrealEmbeddedEngine::connect(endpoint).await {
            Ok(engine) => {
                ENGINES.insert(id, engine).await;
                send_success(vec![], success);
            }
            Err(e) => {
                send_failure(&format!("Cannot connect to db: {}", e.as_str()), failure);
            }
        }
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
