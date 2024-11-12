use std::{error::Error, time::Duration};

fn main() -> Result<(), Box<dyn Error>> {
    csbindgen::Builder::default()
        .input_extern_file("src/lib.rs")
        .input_extern_file("src/bindgen/byte_buffer.rs")
        .input_extern_file("src/bindgen/callback.rs")
        .input_extern_file("src/bindgen/free.rs")
        .input_extern_file("src/models/method.rs")
        .input_extern_file("src/runtime/engines.rs")
        .input_extern_file("src/runtime/mod.rs")
        .csharp_dll_name("surreal_rocksdb")
        .csharp_namespace("SurrealDb.Embedded.Internals")
        .generate_csharp_file("../../SurrealDb.Embedded.RocksDb/NativeMethods.g.cs")?;

    write_surreal_version()?;

    std::thread::sleep(Duration::from_secs(1));

    Ok(())
}

fn write_surreal_version() -> Result<(), Box<dyn Error>> {
    let lock_file = include_str!("../Cargo.lock");
    let lock: cargo_lock::Lockfile = lock_file.parse().expect("Failed to parse Cargo.lock");

    let package = lock
        .packages
        .iter()
        .find(|p| p.name.as_str() == "surrealdb")
        .expect("Failed to find surrealdb in Cargo.lock");

    let version = format!(
        "{}.{}.{}",
        package.version.major, package.version.minor, package.version.patch
    );

    std::fs::write("./src/surreal-version.txt", version)?;

    Ok(())
}
