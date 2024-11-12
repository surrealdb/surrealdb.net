use std::{error::Error, time::Duration};

fn main() -> Result<(), Box<dyn Error>> {
    generate_csharp_file("surreal_memory", "SurrealDb.Embedded.InMemory")?;
    generate_csharp_file("surreal_rocksdb", "SurrealDb.Embedded.RocksDb")?;
    generate_csharp_file("surreal_surrealkv", "SurrealDb.Embedded.SurrealKv")?;

    write_surreal_version()?;

    std::thread::sleep(Duration::from_secs(1));

    Ok(())
}

fn generate_csharp_file(dll_name: &str, csharp_project_name: &str) -> Result<(), Box<dyn Error>> {
    csbindgen::Builder::default()
        .input_extern_file("src/lib.rs")
        .input_extern_file("src/bindgen/byte_buffer.rs")
        .input_extern_file("src/bindgen/callback.rs")
        .input_extern_file("src/bindgen/free.rs")
        .input_extern_file("src/models/method.rs")
        .input_extern_file("src/runtime/engines.rs")
        .input_extern_file("src/runtime/mod.rs")
        .csharp_dll_name(dll_name)
        .csharp_namespace("SurrealDb.Embedded.Internals")
        .generate_csharp_file(format!("../../{}/NativeMethods.g.cs", csharp_project_name))?;

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
