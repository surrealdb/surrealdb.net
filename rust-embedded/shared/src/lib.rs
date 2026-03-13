use app::SurrealEmbeddedEngine;
use bindgen::{
    callback::{FailureAction, SuccessAction, send_failure, send_success},
    csharp_to_rust::{convert_csharp_to_rust_bytes, convert_csharp_to_rust_string_utf16},
};
use models::method::Method;
use runtime::{engines::ENGINES, get_global_runtime};
use uuid::Uuid;

pub mod app;
pub mod bindgen;
pub mod cbor;
pub mod models;
pub mod runtime;

/// # Safety
///
/// Apply connection for the SurrealDB engine (given its id).
/// 💡 "connect" is a reserved keyword
#[unsafe(no_mangle)]
pub unsafe extern "C" fn apply_connect(
    id: i32,
    utf16_str: *const u16,
    utf16_len: i32,
    bytes: *const u8,
    len: i32,
    success: SuccessAction,
    failure: FailureAction,
) {
    let endpoint = unsafe { convert_csharp_to_rust_string_utf16(utf16_str, utf16_len) };
    let opts_bytes = unsafe { convert_csharp_to_rust_bytes(bytes, len) };

    get_global_runtime().spawn(async move {
        match SurrealEmbeddedEngine::connect(endpoint, opts_bytes).await {
            Ok(engine) => {
                ENGINES.insert(id, engine).await;
                send_success(vec![], success);
            }
            Err(e) => {
                send_failure(&format!("Cannot connect to db: {}", e), failure);
            }
        }
    });
}

/// # Safety
///
/// Executes a specific method of a SurrealDB engine (given its id).
/// To execute a method, you should pass down the Method, the params and the callback functions (success, failure).
#[unsafe(no_mangle)]
pub unsafe extern "C" fn execute(
    id: i32,
    method: Method,
    session_bytes: *const u8,
    session_len: i32,
    transaction_bytes: *const u8,
    transaction_len: i32,
    params_bytes: *const u8,
    params_len: i32,
    success: SuccessAction,
    failure: FailureAction,
) {
    let method: surrealdb::rpc::Method = method.into();

    let session_id = if session_len == 16 {
        let session_bytes = unsafe { convert_csharp_to_rust_bytes(session_bytes, session_len) };
        Uuid::try_from(session_bytes).map(Some)
    } else {
        Ok(None)
    };
    if session_id.is_err() {
        send_failure("Failed to deserialize session id", failure);
        return;
    }

    let transaction_id = if transaction_len == 16 {
        let transaction_bytes =
            unsafe { convert_csharp_to_rust_bytes(transaction_bytes, transaction_len) };
        Uuid::try_from(transaction_bytes).map(Some)
    } else {
        Ok(None)
    };
    if transaction_id.is_err() {
        send_failure("Failed to deserialize transaction id", failure);
        return;
    }

    let params_bytes = unsafe { convert_csharp_to_rust_bytes(params_bytes, params_len) };

    get_global_runtime().spawn(async move {
        match ENGINES
            .execute(
                id,
                method,
                session_id.unwrap(),
                transaction_id.unwrap(),
                params_bytes,
            )
            .await
        {
            Ok(output) => {
                send_success(output, success);
            }
            Err(error) => {
                send_failure(&error.to_string(), failure);
            }
        }
    });
}

/// # Safety
///
/// Executes the "import" method of a SurrealDB engine (given its id).
#[unsafe(no_mangle)]
pub unsafe extern "C" fn import(
    id: i32,
    utf16_str: *const u16,
    utf16_len: i32,
    success: SuccessAction,
    failure: FailureAction,
) {
    let input = unsafe { convert_csharp_to_rust_string_utf16(utf16_str, utf16_len) };

    get_global_runtime().spawn(async move {
        match ENGINES.import(id, input).await {
            Ok(_) => {
                send_success(vec![], success);
            }
            Err(error) => {
                send_failure(&error.to_string(), failure);
            }
        }
    });
}

/// # Safety
///
/// Executes the "export" method of a SurrealDB engine (given its id).
#[unsafe(no_mangle)]
pub unsafe extern "C" fn export(
    id: i32,
    bytes: *const u8,
    len: i32,
    success: SuccessAction,
    failure: FailureAction,
) {
    let params_bytes = unsafe { convert_csharp_to_rust_bytes(bytes, len) };

    get_global_runtime().spawn(async move {
        match ENGINES.export(id, params_bytes).await {
            Ok(output) => {
                send_success(output, success);
            }
            Err(error) => {
                send_failure(&error.to_string(), failure);
            }
        }
    });
}
