use cargo_metadata::MetadataCommand;
use surrealdb::sql::{Strand, Value};

use crate::bindgen::callback::{send_failure, send_success, FailureAction, SuccessAction};

fn get_version() -> Result<String, String> {
    let path = env!("CARGO_MANIFEST_DIR");

    let metadata = MetadataCommand::new()
        .manifest_path("./Cargo.toml")
        .current_dir(path)
        .exec()
        .map_err(|e| e.to_string())?;

    metadata
        .packages
        .iter()
        .find(|d| d.name == "surrealdb")
        .ok_or_else(|| "Cannot surrealdb package".to_string())
        .map(|package| package.version.to_string())
}

pub async fn version_async(success: SuccessAction, failure: FailureAction) {
    match get_version() {
        Ok(value) => {
            let value = Value::Strand(Strand::from(value));
            send_success(value, success, failure);
        }
        Err(error) => {
            send_failure(error.to_string().as_str(), failure);
        }
    }
}
