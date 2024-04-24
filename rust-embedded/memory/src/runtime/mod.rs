pub mod db;

use once_cell::sync::OnceCell;
use tokio::runtime::{Builder, Runtime};

pub static mut RUNTIME: OnceCell<Runtime> = OnceCell::new();

#[no_mangle]
pub unsafe extern "C" fn create_global_runtime() {
    unsafe {
        RUNTIME
            .set(
                Builder::new_multi_thread()
                    .worker_threads(num_cpus::get())
                    .enable_all()
                    .build()
                    .unwrap(),
            )
            .unwrap();
    }
}

pub fn get_global_runtime<'local>() -> &'local Runtime {
    unsafe { RUNTIME.get_unchecked() }
}
