use anyhow::anyhow;
use dashmap::DashMap;
use std::collections::BTreeMap;
use std::sync::Arc;
use surrealdb::dbs::Session;
use surrealdb::kvs::Datastore;
use surrealdb::rpc::format::cbor::encode;
use surrealdb::rpc::{RpcProtocol, types_error_from_anyhow};
use surrealdb_datastore::{Transaction, TransactionType};
use surrealdb_rpc::error::invalid_params;
use surrealdb_rpc::export::Config;
use surrealdb_rpc::{DbResult, Method};
use surrealdb_types::{Array, HashMap, SurrealValue, Value};
use tokio::sync::RwLock;
use uuid::Uuid;

use crate::cbor::decode;
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
        session_id: Option<Uuid>,
        transaction_id: Option<Uuid>,
        params: Vec<u8>,
    ) -> anyhow::Result<Vec<u8>> {
        let read_lock = self.0.read().await;
        let Some(engine) = read_lock.get(&id) else {
            return Err(anyhow!("Engine not found"));
        };
        engine
            .execute(method, session_id, transaction_id, params)
            .await
    }

    pub async fn import(&self, id: i32, input: String) -> anyhow::Result<()> {
        let read_lock = self.0.read().await;
        let Some(engine) = read_lock.get(&id) else {
            return Err(anyhow!("Engine not found"));
        };
        engine.import(input).await
    }

    pub async fn export(&self, id: i32, params: Vec<u8>) -> anyhow::Result<Vec<u8>> {
        let read_lock = self.0.read().await;
        let Some(engine) = read_lock.get(&id) else {
            return Err(anyhow!("Engine not found"));
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
    pub async fn execute(
        &self,
        method: Method,
        session_id: Option<Uuid>,
        transaction_id: Option<Uuid>,
        params: Vec<u8>,
    ) -> anyhow::Result<Vec<u8>> {
        let params =
            crate::cbor::get_params(params).map_err(|_| anyhow!("Failed to deserialize params"))?;
        let rpc = self.0.read().await;
        // A request naming no session runs under this engine's implicit default session.
        let session = session_id.unwrap_or(rpc.default_session);
        let res = RpcProtocol::execute(&*rpc, transaction_id, session, session_id, method, params)
            .await?;
        encode(res.into_value())
    }

    pub async fn connect(
        endpoint: String,
        options: Vec<u8>,
    ) -> anyhow::Result<SurrealEmbeddedEngine> {
        let endpoint = match &endpoint {
            s if s.starts_with("mem:") => "memory",
            s => s,
        };

        let in_options = decode(&options)?;
        let options = ConnectionOptions::try_from(&in_options).map_err(|e| anyhow!(e))?;

        // Live queries are unsupported (`LQ_SUPPORT = false`), so no notification channel is
        // wired. Maintenance tasks are started by hand, after the storage version has been
        // checked (and any pending migrations run), as the 3.3 local engine does.
        let kvs = Datastore::builder()
            .with_capabilities(
                options
                    .capabilities
                    .map_or(Ok(Default::default()), |a| a.try_into())?,
            )
            .without_maintenance_tasks()
            .build_with_path(endpoint)
            .await?;
        kvs.check_version().await?;
        kvs.bootstrap().await?;
        kvs.start_maintenance_tasks();

        // Register the implicit default session, used by every request that names no session.
        let default_session = Uuid::now_v7();
        let inner = SurrealEmbeddedEngineInner {
            kvs,
            default_session,
            sessions: HashMap::new(),
            transactions: DashMap::new(),
        };
        // Its `id` is left unset, as it was before 3.3, so `$session.id` stays NONE for it.
        inner
            .sessions
            .insert(default_session, Arc::new(RwLock::new(Session::default())));

        Ok(SurrealEmbeddedEngine(RwLock::new(inner)))
    }

    pub async fn export(&self, config: Vec<u8>) -> anyhow::Result<Vec<u8>> {
        let (tx, rx) = channel::unbounded();

        let inner = self.0.read().await;

        let in_config = decode(&config)?;
        let config = Config::from_value(in_config)?;

        let lock = inner.get_session(&inner.default_session).await?;
        let session = lock.read().await;

        inner
            .kvs
            .export_with_config(&session, tx, config)
            .await?
            .await?;

        let mut buffer = Vec::new();
        while let Ok(item) = rx.try_recv() {
            buffer.push(item);
        }

        let result = String::from_utf8(buffer.concat()).map_err(|e| anyhow!(e))?;
        encode(result.into_value())
    }

    /// Stops the maintenance tasks and closes the storage engine, releasing its files.
    pub async fn shutdown(&self) {
        let inner = self.0.read().await;
        // Open transactions would otherwise keep the storage busy
        inner.transactions.clear();
        inner.kvs.shutdown().await.ok();
    }

    pub async fn import(&self, input: String) -> anyhow::Result<()> {
        let inner = self.0.read().await;

        let lock = inner.get_session(&inner.default_session).await?;
        let session = lock.write().await;

        inner.kvs.import(&input, &session).await?;

        Ok(())
    }
}

struct SurrealEmbeddedEngineInner {
    pub kvs: Arc<Datastore>,
    /// The id of the implicit default session, used by every request that names no session.
    pub default_session: Uuid,
    pub sessions: HashMap<Uuid, Arc<RwLock<Session>>>,
    pub transactions: DashMap<Uuid, Arc<Transaction>>,
}

type TxError = surrealdb_types::Error;
type TxResult<T> = std::result::Result<T, TxError>;

impl RpcProtocol for SurrealEmbeddedEngineInner {
    fn kvs(&self) -> &Datastore {
        &self.kvs
    }

    fn kvs_arc(&self) -> Arc<Datastore> {
        Arc::clone(&self.kvs)
    }

    fn session_map(&self) -> &HashMap<Uuid, Arc<RwLock<Session>>> {
        &self.sessions
    }

    /// Lists the attached sessions, leaving out the implicit default session.
    async fn sessions(&self) -> TxResult<DbResult> {
        let sessions = self
            .session_map()
            .to_vec()
            .into_iter()
            .filter(|(id, _)| *id != self.default_session)
            .map(|(id, _)| Value::Uuid(surrealdb_types::Uuid::from(id)))
            .collect();
        Ok(DbResult::Other(Value::Array(sessions)))
    }

    fn version_data(&self) -> DbResult {
        DbResult::Other(Value::String(format!("surrealdb-{}", SURREALDB_VERSION)))
    }

    const LQ_SUPPORT: bool = false;

    async fn cleanup_lqs(&self, _: &Uuid) {}
    async fn cleanup_all_lqs(&self) {}

    // ------------------------------
    // Transactions
    // ------------------------------

    /// Retrieves a transaction by ID
    async fn get_tx(&self, id: Uuid) -> TxResult<Arc<Transaction>> {
        self.transactions
            .get(&id)
            .map(|tx| tx.clone())
            .ok_or_else(|| invalid_params("Transaction not found"))
    }

    /// Stores a transaction
    async fn set_tx(&self, id: Uuid, tx: Arc<Transaction>) -> TxResult<()> {
        self.transactions.insert(id, tx);
        Ok(())
    }

    // ------------------------------
    // Methods for transactions
    // ------------------------------

    /// Begin a new transaction
    async fn begin(&self, _txn: Option<Uuid>, _session_id: Uuid) -> TxResult<DbResult> {
        // Create a new transaction
        let tx = self
            .kvs()
            .transaction(TransactionType::Write)
            .await
            .map_err(types_error_from_anyhow)?;
        // Generate a unique transaction ID
        let id = Uuid::now_v7();
        // Store the transaction in the map
        self.transactions.insert(id, Arc::new(tx));
        // Return the transaction ID to the client
        Ok(DbResult::Other(Value::Uuid(surrealdb_types::Uuid::from(
            id,
        ))))
    }

    /// Commit a transaction
    async fn commit(
        &self,
        _txn: Option<Uuid>,
        _session_id: Uuid,
        params: Array,
    ) -> TxResult<DbResult> {
        let mut params_vec = params.into_vec();
        let Some(Value::Uuid(txn_id)) = params_vec.pop() else {
            return Err(invalid_params("Expected transaction UUID"));
        };
        let txn_id = txn_id.into_inner();
        let Some((_, tx)) = self.transactions.remove(&txn_id) else {
            return Err(invalid_params("Transaction not found"));
        };
        tx.commit().await.map_err(types_error_from_anyhow)?;
        Ok(DbResult::Other(Value::None))
    }

    /// Cancel a transaction
    async fn cancel(
        &self,
        _txn: Option<Uuid>,
        _session_id: Uuid,
        params: Array,
    ) -> TxResult<DbResult> {
        let mut params_vec = params.into_vec();
        let Some(Value::Uuid(txn_id)) = params_vec.pop() else {
            return Err(invalid_params("Expected transaction UUID"));
        };
        let txn_id = txn_id.into_inner();
        let Some((_, tx)) = self.transactions.remove(&txn_id) else {
            return Err(invalid_params("Transaction not found"));
        };
        tx.cancel().await.map_err(types_error_from_anyhow)?;

        // Return success
        Ok(DbResult::Other(Value::None))
    }
}

static SURREALDB_VERSION: &str = include_str!("../surreal-version.txt");
