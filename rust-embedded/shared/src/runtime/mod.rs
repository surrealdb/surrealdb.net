pub mod engines;

use once_cell::sync::OnceCell;
use tokio::runtime::{Builder, Runtime};

pub static RUNTIME: OnceCell<Runtime> = OnceCell::new();

/// # Safety
///
/// This function is called to initialize the async runtime (using tokio).
#[unsafe(no_mangle)]
pub extern "C" fn create_global_runtime() {
    RUNTIME
        .set(
            Builder::new_multi_thread()
                .worker_threads(num_cpus::get())
                .enable_all()
                .build()
                .unwrap(),
        )
        .unwrap()
}

pub fn get_global_runtime<'local>() -> &'local Runtime {
    unsafe { RUNTIME.get_unchecked() }
}
