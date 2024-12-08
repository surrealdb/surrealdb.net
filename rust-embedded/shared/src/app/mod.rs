use std::collections::BTreeMap;
use surrealdb::dbs::Session;
use surrealdb::kvs::export::Config;
use surrealdb::kvs::Datastore;
use surrealdb::rpc::format::cbor;
use surrealdb::rpc::method::Method;
use surrealdb::rpc::{Data, RpcContext};
use surrealdb::sql::Value;
use tokio::sync::RwLock;
use uuid::Uuid;

pub use crate::err::Error;

pub struct SurrealEmbeddedEngines(RwLock<BTreeMap<i32, SurrealEmbeddedEngine>>);

impl SurrealEmbeddedEngines {
    pub fn new() -> Self {
        SurrealEmbeddedEngines(RwLock::new(Default::default()))
    }

    pub async fn execute(
        &self,
        id: i32,
        method: Method,
        params: Vec<u8>,
    ) -> Result<Vec<u8>, Error> {
        let read_lock = self.0.read().await;
        let Some(engine) = read_lock.get(&id) else {
            return Err("Engine not found".into());
        };
        engine.execute(method, params).await
    }

    pub async fn export(&self, id: i32, params: Vec<u8>) -> Result<Vec<u8>, Error> {
        let read_lock = self.0.read().await;
        let Some(engine) = read_lock.get(&id) else {
            return Err("Engine not found".into());
        };
        engine.export(params).await
    }

    pub async fn insert(
        &self,
        id: i32,
        engine: SurrealEmbeddedEngine,
    ) -> Option<SurrealEmbeddedEngine> {
        self.0.write().await.insert(id, engine)
    }

    pub async fn remove(&self, id: i32) -> Option<SurrealEmbeddedEngine> {
        self.0.write().await.remove(&id)
    }
}

impl Default for SurrealEmbeddedEngines {
    fn default() -> Self {
        Self::new()
    }
}

pub struct SurrealEmbeddedEngine(RwLock<SurrealEmbeddedEngineInner>);

impl SurrealEmbeddedEngine {
    pub async fn execute(&self, method: Method, params: Vec<u8>) -> Result<Vec<u8>, Error> {
        let params = crate::cbor::get_params(params)
            .map_err(|_| "Failed to deserialize params".to_string())?;
        let res = self
            .0
            .write()
            .await
            .execute(method, params)
            .await
            .map_err(|e| e.to_string())?;
        let out = cbor::res(res).map_err(|e| e.to_string())?;
        Ok(out)
    }

    pub async fn connect(endpoint: String) -> Result<SurrealEmbeddedEngine, Error> {
        let endpoint = match &endpoint {
            s if s.starts_with("mem:") => "memory",
            s => s,
        };
        let kvs = Datastore::new(endpoint).await?.with_notifications();

        let inner = SurrealEmbeddedEngineInner {
            kvs,
            session: Session::default().with_rt(true),
            vars: Default::default(),
        };

        Ok(SurrealEmbeddedEngine(RwLock::new(inner)))
    }

    pub async fn export(&self, config: Vec<u8>) -> Result<Vec<u8>, Error> {
        let (tx, rx) = channel::unbounded();

        let inner = self.0.read().await;

        let in_config = cbor::parse_value(config.to_vec()).map_err(|e| e.to_string())?;
        let config = Config::try_from(&in_config).map_err(|e| e.to_string())?;

        inner
            .kvs
            .export_with_config(&inner.session, tx, config)
            .await?
            .await?;

        let mut buffer = Vec::new();
        while let Ok(item) = rx.try_recv() {
            buffer.push(item);
        }

		let result = String::from_utf8(buffer.concat()).map_err(|e| e.to_string())?;
        
        let out = cbor::res(result).map_err(|e| e.to_string())?;
        Ok(out)
    }
}

struct SurrealEmbeddedEngineInner {
    pub kvs: Datastore,
    pub session: Session,
    pub vars: BTreeMap<String, Value>,
}

impl RpcContext for SurrealEmbeddedEngineInner {
    fn kvs(&self) -> &Datastore {
        &self.kvs
    }

    fn session(&self) -> &Session {
        &self.session
    }

    fn session_mut(&mut self) -> &mut Session {
        &mut self.session
    }

    fn vars(&self) -> &BTreeMap<String, Value> {
        &self.vars
    }

    fn vars_mut(&mut self) -> &mut BTreeMap<String, Value> {
        &mut self.vars
    }

    fn version_data(&self) -> Data {
        Value::from(format!("surrealdb-{}", SURREALDB_VERSION)).into()
    }

    const LQ_SUPPORT: bool = true;
    async fn handle_live(&self, _lqid: &Uuid) {}
    async fn handle_kill(&self, _lqid: &Uuid) {}
}

static SURREALDB_VERSION: &str = include_str!("../surreal-version.txt");
