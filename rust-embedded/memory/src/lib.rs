use ::surrealdb::sql::{Array, Value};
use bindgen::{
    callback::{send_failure, send_success, FailureAction, SuccessAction},
    csharp_to_rust::convert_csharp_to_rust_bytes,
};
use methods::{
    create::create_async, delete::delete_async, merge::merge_async, patch::patch_async,
    ping::ping_async, query::query_async, select::select_async, set::set_async, unset::unset_async,
    update::update_async, version::version_async,
};
use models::method::Method;
use runtime::{db::get_db, get_global_runtime};

use crate::methods::use_ns_db::use_ns_db_async;

mod bindgen;
mod cbor;
mod methods;
mod models;
mod runtime;

fn read_params(bytes: *const u8, len: i32) -> Result<Array, ()> {
    let bytes = convert_csharp_to_rust_bytes(bytes, len);
    cbor::get_params(bytes)
}

// 💡 "connect" is a reserved keyword
#[no_mangle]
pub extern "C" fn apply_connect(
    id: i32,
    success: SuccessAction,
    failure: FailureAction,
) {
    // 💡 connect is only used to ensure database can be created (sort of avoiding cold start)
    get_global_runtime().spawn(async move {
        let Ok(_) = get_db(id).await else {
            send_failure("Cannot retrieve db", failure);
            return;
        };
        send_success(Value::None, success, failure);
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
    match method {
        Method::Ping => {
            get_global_runtime().spawn(async move {
                let Ok(db) = get_db(id).await else {
                    send_failure("Cannot retrieve db", failure);
                    return;
                };
                ping_async(db, success, failure).await;
            });
        }
        Method::Use => {
            if let Ok(params) = read_params(bytes, len) {
                get_global_runtime().spawn(async move {
                    let Ok(db) = get_db(id).await else {
                        send_failure("Cannot retrieve db", failure);
                        return;
                    };
                    use_ns_db_async(db, params, success, failure).await;
                });
            } else {
                send_failure("Cannot retrieve params", failure);
            }
        }
        Method::Set => {
            if let Ok(params) = read_params(bytes, len) {
                get_global_runtime().spawn(async move {
                    let Ok(db) = get_db(id).await else {
                        send_failure("Cannot retrieve db", failure);
                        return;
                    };
                    set_async(db, params, success, failure).await;
                });
            } else {
                send_failure("Cannot retrieve params", failure);
            }
        }
        Method::Unset => {
            if let Ok(params) = read_params(bytes, len) {
                get_global_runtime().spawn(async move {
                    let Ok(db) = get_db(id).await else {
                        send_failure("Cannot retrieve db", failure);
                        return;
                    };
                    unset_async(db, params, success, failure).await;
                });
            } else {
                send_failure("Cannot retrieve params", failure);
            }
        }
        Method::Select => {
            if let Ok(params) = read_params(bytes, len) {
                get_global_runtime().spawn(async move {
                    let Ok(db) = get_db(id).await else {
                        send_failure("Cannot retrieve db", failure);
                        return;
                    };
                    select_async(db, params, success, failure).await;
                });
            } else {
                send_failure("Cannot retrieve params", failure);
            }
        }
        Method::Create => {
            if let Ok(params) = read_params(bytes, len) {
                get_global_runtime().spawn(async move {
                    let Ok(db) = get_db(id).await else {
                        send_failure("Cannot retrieve db", failure);
                        return;
                    };
                    create_async(db, params, success, failure).await;
                });
            } else {
                send_failure("Cannot retrieve params", failure);
            }
        }
        Method::Update => {
            if let Ok(params) = read_params(bytes, len) {
                get_global_runtime().spawn(async move {
                    let Ok(db) = get_db(id).await else {
                        send_failure("Cannot retrieve db", failure);
                        return;
                    };
                    update_async(db, params, success, failure).await;
                });
            } else {
                send_failure("Cannot retrieve params", failure);
            }
        }
        Method::Merge => {
            if let Ok(params) = read_params(bytes, len) {
                get_global_runtime().spawn(async move {
                    let Ok(db) = get_db(id).await else {
                        send_failure("Cannot retrieve db", failure);
                        return;
                    };
                    merge_async(db, params, success, failure).await;
                });
            } else {
                send_failure("Cannot retrieve params", failure);
            }
        }
        Method::Patch => {
            if let Ok(params) = read_params(bytes, len) {
                get_global_runtime().spawn(async move {
                    let Ok(db) = get_db(id).await else {
                        send_failure("Cannot retrieve db", failure);
                        return;
                    };
                    patch_async(db, params, success, failure).await;
                });
            } else {
                send_failure("Cannot retrieve params", failure);
            }
        }
        Method::Delete => {
            if let Ok(params) = read_params(bytes, len) {
                get_global_runtime().spawn(async move {
                    let Ok(db) = get_db(id).await else {
                        send_failure("Cannot retrieve db", failure);
                        return;
                    };
                    delete_async(db, params, success, failure).await;
                });
            } else {
                send_failure("Cannot retrieve params", failure);
            }
        }
        Method::Version => {
            get_global_runtime().spawn(async move {
                version_async(success, failure).await;
            });
        }
        Method::Query => {
            if let Ok(params) = read_params(bytes, len) {
                get_global_runtime().spawn(async move {
                    let Ok(db) = get_db(id).await else {
                        send_failure("Cannot retrieve db", failure);
                        return;
                    };
                    query_async(db, params, success, failure).await;
                });
            } else {
                send_failure("Cannot retrieve params", failure);
            }
        },
    }
}
