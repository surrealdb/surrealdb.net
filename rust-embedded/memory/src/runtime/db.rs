use once_cell::sync::Lazy;
use std::{
    collections::HashMap,
    sync::{Arc, RwLock},
};
use surrealdb::{
    engine::local::{Db, Mem},
    Surreal,
};

type SurrealDbInstances = HashMap<i32, Arc<Surreal<Db>>>;

static DBS: Lazy<Arc<RwLock<SurrealDbInstances>>> =
    Lazy::new(|| Arc::new(RwLock::new(SurrealDbInstances::new())));

pub async fn get_db(id: i32) -> Result<Arc<Surreal<Db>>, String> {
    {
        // Scope to ensure the read lock is dropped before the await.
        let dbs = DBS.read().map_err(|e| e.to_string())?;
        if let Some(db) = dbs.get(&id) {
            return Ok(Arc::clone(db));
        }
    } // The read lock is dropped here.

    let db = Surreal::new::<Mem>(()).await.map_err(|e| e.to_string())?;
    let db_arc = Arc::new(db);
    let mut write_dbs = DBS.write().unwrap();
    write_dbs.insert(id, Arc::clone(&db_arc));
    Ok(db_arc)
}

#[no_mangle]
pub extern "C" fn dispose(id: i32) {
    // TODO : impl drop for Surreal
    DBS.write().unwrap().remove(&id);
}
