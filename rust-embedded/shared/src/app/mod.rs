use anyhow::anyhow;
use dashmap::DashMap;
use std::collections::BTreeMap;
use std::sync::Arc;
use surrealdb::dbs::Session;
use surrealdb::kvs::export::Config;
use surrealdb::kvs::{self, Datastore, LockType, Transaction, TransactionType};
use surrealdb::rpc::format::cbor::{decode, encode};
use surrealdb::rpc::{self, DbResult, Method, RpcProtocol};
use surrealdb_types::{Array, HashMap, SurrealValue, Value};
use tokio::sync::RwLock;
use uuid::Uuid;

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
        let res = RpcProtocol::execute(&*rpc, transaction_id, session_id, method, params).await?;
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

        let kvs = Datastore::new(endpoint)
            .await?
            .with_notifications()
            .with_capabilities(
                options
                    .capabilities
                    .map_or(Ok(Default::default()), |a| a.try_into())?,
            );

        let inner = SurrealEmbeddedEngineInner {
            kvs,
            sessions: HashMap::new(),
            transactions: DashMap::new(),
        };
        // Store the default session with None key
        let session = Session::default();
        inner.sessions.insert(None, Arc::new(RwLock::new(session)));

        Ok(SurrealEmbeddedEngine(RwLock::new(inner)))
    }

    pub async fn export(&self, config: Vec<u8>) -> anyhow::Result<Vec<u8>> {
        let (tx, rx) = channel::unbounded();

        let inner = self.0.read().await;

        let in_config = decode(&config)?;
        let config = Config::from_value(in_config)?;

        let lock = inner.get_session(&None)?;
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

    pub async fn import(&self, input: String) -> anyhow::Result<()> {
        let inner = self.0.read().await;

        let lock = inner.get_session(&None)?;
        let session = lock.write().await;

        inner.kvs.import(&input, &session).await?;

        Ok(())
    }
}

struct SurrealEmbeddedEngineInner {
    pub kvs: Datastore,
    pub sessions: HashMap<Option<Uuid>, Arc<RwLock<Session>>>,
    pub transactions: DashMap<Uuid, Arc<Transaction>>,
}

type TxError = surrealdb_types::Error;
type TxResult<T> = std::result::Result<T, TxError>;

impl RpcProtocol for SurrealEmbeddedEngineInner {
    fn kvs(&self) -> &Datastore {
        &self.kvs
    }

    fn session_map(&self) -> &HashMap<Option<Uuid>, Arc<RwLock<Session>>> {
        &self.sessions
    }

    fn version_data(&self) -> DbResult {
        DbResult::Other(Value::String(format!("surrealdb-{}", SURREALDB_VERSION)))
    }

    const LQ_SUPPORT: bool = false;

    async fn cleanup_lqs(&self, _: Option<&Uuid>) {}
    async fn cleanup_all_lqs(&self) {}

    // ------------------------------
    // Transactions
    // ------------------------------

    /// Retrieves a transaction by ID
    async fn get_tx(&self, id: Uuid) -> TxResult<Arc<kvs::Transaction>> {
        self.transactions
            .get(&id)
            .map(|tx| tx.clone())
            .ok_or_else(|| rpc::invalid_params("Transaction not found"))
    }

    /// Stores a transaction
    async fn set_tx(&self, id: Uuid, tx: Arc<kvs::Transaction>) -> TxResult<()> {
        self.transactions.insert(id, tx);
        Ok(())
    }

    // ------------------------------
    // Methods for transactions
    // ------------------------------

    /// Begin a new transaction
    async fn begin(&self, _txn: Option<Uuid>, _session_id: Option<Uuid>) -> TxResult<DbResult> {
        // Create a new transaction
        let tx = self
            .kvs()
            .transaction(TransactionType::Write, LockType::Optimistic)
            .await
            .map_err(rpc::types_error_from_anyhow)?;
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
        _session_id: Option<Uuid>,
        params: Array,
    ) -> TxResult<DbResult> {
        let mut params_vec = params.into_vec();
        let Some(Value::Uuid(txn_id)) = params_vec.pop() else {
            return Err(rpc::invalid_params("Expected transaction UUID"));
        };
        let txn_id = txn_id.into_inner();
        let Some((_, tx)) = self.transactions.remove(&txn_id) else {
            return Err(rpc::invalid_params("Transaction not found"));
        };
        tx.commit().await.map_err(rpc::types_error_from_anyhow)?;
        Ok(DbResult::Other(Value::None))
    }

    /// Cancel a transaction
    async fn cancel(
        &self,
        _txn: Option<Uuid>,
        _session_id: Option<Uuid>,
        params: Array,
    ) -> TxResult<DbResult> {
        let mut params_vec = params.into_vec();
        let Some(Value::Uuid(txn_id)) = params_vec.pop() else {
            return Err(rpc::invalid_params("Expected transaction UUID"));
        };
        let txn_id = txn_id.into_inner();
        let Some((_, tx)) = self.transactions.remove(&txn_id) else {
            return Err(rpc::invalid_params("Transaction not found"));
        };
        tx.cancel().await.map_err(rpc::types_error_from_anyhow)?;

        // Return success
        Ok(DbResult::Other(Value::None))
    }
}

static SURREALDB_VERSION: &str = include_str!("../surreal-version.txt");
