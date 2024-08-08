use once_cell::sync::Lazy;

use crate::app::SurrealEmbeddedEngines;

use super::get_global_runtime;

pub static ENGINES: Lazy<SurrealEmbeddedEngines> = Lazy::new(SurrealEmbeddedEngines::new);

#[no_mangle]
pub extern "C" fn dispose(id: i32) {
    // TODO : impl drop for Surreal
    get_global_runtime().spawn(async move {
        ENGINES.remove(id).await;
    });
}
