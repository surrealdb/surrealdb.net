use std::{error::Error, time::Duration};

fn main() -> Result<(), Box<dyn Error>> {
    csbindgen::Builder::default()
        .input_extern_file("src/lib.rs")
        .input_extern_file("src/bindgen/byte_buffer.rs")
        .input_extern_file("src/bindgen/callback.rs")
        .input_extern_file("src/bindgen/free.rs")
        .input_extern_file("src/models/method.rs")
        .input_extern_file("src/runtime/db.rs")
        .input_extern_file("src/runtime/mod.rs")
        .csharp_dll_name("surreal_memory")
        .csharp_namespace("SurrealDb.Embedded.InMemory.Internals")
        .generate_csharp_file("../../SurrealDb.Embedded.InMemory/NativeMethods.g.cs")?;

    std::thread::sleep(Duration::from_secs(1));

    Ok(())
}
