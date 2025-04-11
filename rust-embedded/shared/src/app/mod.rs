use arc_swap::ArcSwap;
use std::collections::BTreeMap;
use std::sync::Arc;
use surrealdb::dbs::Session;
use surrealdb::kvs::export::Config;
use surrealdb::kvs::Datastore;
use surrealdb::rpc::format::cbor;
use surrealdb::rpc::Method;
use surrealdb::rpc::{Data, RpcContext, RpcProtocolV1, RpcProtocolV2};
use surrealdb::sql::Value;
use tokio::sync::{RwLock, Semaphore};
use uuid::Uuid;

pub use crate::err::Error;
use crate::models::connection_options::ConnectionOptions;

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

    pub async fn import(&self, id: i32, input: String) -> Result<(), Error> {
        let read_lock = self.0.read().await;
        let Some(engine) = read_lock.get(&id) else {
            return Err("Engine not found".into());
        };
        engine.import(input).await
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
        let rpc = self.0.read().await;
        let res = RpcContext::execute(&*rpc, None, method, params)
            .await
            .map_err(|e| e.to_string())?;
        let value: Value = res.try_into().map_err(|e: surrealdb::err::Error| e.to_string())?;
        let out = cbor::res(value).map_err(|e| e.to_string())?;
        Ok(out)
    }

    pub async fn connect(
        endpoint: String,
        options: Vec<u8>,
    ) -> Result<SurrealEmbeddedEngine, Error> {
        let endpoint = match &endpoint {
            s if s.starts_with("mem:") => "memory",
            s => s,
        };

        let in_options = cbor::parse_value(options).map_err(|e| e.to_string())?;
        let options = ConnectionOptions::try_from(&in_options).map_err(|e| e.to_string())?;

        let kvs = Datastore::new(endpoint)
            .await?
            .with_notifications()
            .with_capabilities(
                options
                    .capabilities
                    .map_or(Ok(Default::default()), |a| a.try_into())?,
            )
            .with_strict_mode(options.strict.map_or(Default::default(), |s| s));

        let inner = SurrealEmbeddedEngineInner {
            kvs,
            lock: Arc::new(Semaphore::new(1)),
            session: ArcSwap::from(Arc::new(Session::default().with_rt(true))),
        };

        Ok(SurrealEmbeddedEngine(RwLock::new(inner)))
    }

    pub async fn export(&self, config: Vec<u8>) -> Result<Vec<u8>, Error> {
        let (tx, rx) = channel::unbounded();

        let inner = self.0.read().await;

        let in_config = cbor::parse_value(config).map_err(|e| e.to_string())?;
        let config = Config::try_from(&in_config).map_err(|e| e.to_string())?;

        inner
            .kvs
            .export_with_config(&inner.session(), tx, config)
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

    pub async fn import(&self, input: String) -> Result<(), Error> {
        let inner = self.0.read().await;
        inner.kvs.import(&input, &inner.session()).await?;

        Ok(())
    }
}

struct SurrealEmbeddedEngineInner {
    pub kvs: Datastore,
    pub lock: Arc<Semaphore>,
    pub session: ArcSwap<Session>,
}

impl RpcProtocolV1 for SurrealEmbeddedEngineInner {}
impl RpcProtocolV2 for SurrealEmbeddedEngineInner {}

impl RpcContext for SurrealEmbeddedEngineInner {
    fn kvs(&self) -> &Datastore {
        &self.kvs
    }

    fn lock(&self) -> Arc<Semaphore> {
        self.lock.clone()
    }

    fn session(&self) -> Arc<Session> {
        self.session.load_full()
    }

    fn set_session(&self, session: Arc<Session>) {
        self.session.store(session);
    }

    fn version_data(&self) -> Data {
        Value::from(format!("surrealdb-{}", SURREALDB_VERSION)).into()
    }

    const LQ_SUPPORT: bool = true;
    async fn handle_live(&self, _lqid: &Uuid) {}
    async fn handle_kill(&self, _lqid: &Uuid) {}
}

static SURREALDB_VERSION: &str = include_str!("../surreal-version.txt");
