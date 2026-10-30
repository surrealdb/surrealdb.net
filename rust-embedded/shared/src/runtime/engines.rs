use once_cell::sync::Lazy;

use crate::app::SurrealEmbeddedEngines;

use super::get_global_runtime;

pub static ENGINES: Lazy<SurrealEmbeddedEngines> = Lazy::new(SurrealEmbeddedEngines::new);

/// Shuts the engine down and waits for it, so that its storage can be opened again right away.
#[unsafe(no_mangle)]
pub extern "C" fn dispose(id: i32) {
    let shutdown = async move {
        if let Some(engine) = ENGINES.remove(id).await {
            engine.shutdown().await;
        }
    };

    // Blocking is only possible outside the runtime: a call from one of its own threads shuts down in the background
    if tokio::runtime::Handle::try_current().is_ok() {
        get_global_runtime().spawn(shutdown);
    } else {
        get_global_runtime().block_on(shutdown);
    }
}
