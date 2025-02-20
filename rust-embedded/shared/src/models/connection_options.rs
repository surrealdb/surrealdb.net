use std::collections::HashSet;
use surrealdb::{dbs::capabilities, err::Error, sql::Value};

#[derive(Debug, Default)]
pub struct ConnectionOptions {
    pub strict: Option<bool>,
    pub capabilities: Option<CapabilitiesConfig>,
}

#[derive(Debug, Default)]
pub struct CapabilitiesConfig {
    pub experimental: Option<Targets>,
}

#[derive(Debug, Default)]
pub struct Targets {
    pub allow: Option<TargetsConfig>,
    pub deny: Option<TargetsConfig>,
}

#[derive(Debug, Default)]
pub struct TargetsConfig {
    /// allow all or deny all if array is not defined
    pub bool: Option<bool>,
    pub array: Option<HashSet<String>>,
}

impl TryFrom<&Value> for ConnectionOptions {
    type Error = Error;
    fn try_from(value: &Value) -> Result<Self, Self::Error> {
        match value {
            Value::None | Value::Null => Ok(ConnectionOptions::default()),
            Value::Object(obj) => {
                let mut connection = ConnectionOptions::default();

                match obj.get("strict") {
                    Some(Value::None) => (),
                    Some(Value::Bool(v)) => {
                        connection.strict = Some(v.to_owned());
                    }
                    Some(v) => {
                        return Err(Error::ConvertTo {
                            from: v.to_owned(),
                            into: "bool".to_string(),
                        })
                    }
                    _ => (),
                }

                if let Some(v) = obj.get("capabilities") {
                    connection.capabilities = Some(v.try_into()?);
                }

                Ok(connection)
            }
            v => Err(Error::ConvertTo {
                from: v.to_owned(),
                into: "object".to_string(),
            }),
        }
    }
}

impl TryFrom<&Value> for CapabilitiesConfig {
    type Error = Error;
    fn try_from(value: &Value) -> Result<Self, Self::Error> {
        match value {
            Value::None | Value::Null => Ok(CapabilitiesConfig::default()),
            Value::Object(obj) => {
                let mut config = CapabilitiesConfig::default();

                if let Some(v) = obj.get("experimental") {
                    config.experimental = Some(v.try_into()?);
                }

                Ok(config)
            }
            v => Err(Error::ConvertTo {
                from: v.to_owned(),
                into: "object".to_string(),
            }),
        }
    }
}

impl TryFrom<&Value> for Targets {
    type Error = Error;
    fn try_from(value: &Value) -> Result<Self, Self::Error> {
        match value {
            Value::None | Value::Null => Ok(Targets::default()),
            Value::Object(obj) => {
                let mut targets = Targets::default();

                if let Some(v) = obj.get("allow") {
                    targets.allow = Some(v.try_into()?);
                }

                if let Some(v) = obj.get("deny") {
                    targets.deny = Some(v.try_into()?);
                }

                Ok(targets)
            }
            v => Err(Error::ConvertTo {
                from: v.to_owned(),
                into: "object".to_string(),
            }),
        }
    }
}

impl TryFrom<&Value> for TargetsConfig {
    type Error = Error;
    fn try_from(value: &Value) -> Result<Self, Self::Error> {
        match value {
            Value::None | Value::Null => Ok(TargetsConfig::default()),
            Value::Object(obj) => {
                let mut config = TargetsConfig::default();

                match obj.get("bool") {
                    Some(Value::None) => (),
                    Some(Value::Bool(v)) => {
                        config.bool = Some(v.to_owned());
                    }
                    Some(v) => {
                        return Err(Error::ConvertTo {
                            from: v.to_owned(),
                            into: "bool".to_string(),
                        })
                    }
                    _ => (),
                }

                match obj.get("array") {
                    Some(Value::None) => (),
                    Some(Value::Array(v)) => {
                        config.array = Some(v.iter().map(|v| v.to_string()).collect());
                    }
                    Some(v) => {
                        return Err(Error::ConvertTo {
                            from: v.to_owned(),
                            into: "array".to_string(),
                        })
                    }
                    _ => (),
                }

                Ok(config)
            }
            v => Err(Error::ConvertTo {
                from: v.to_owned(),
                into: "object".to_string(),
            }),
        }
    }
}

macro_rules! process_targets {
    ($set:ident) => {{
        let mut functions = HashSet::with_capacity($set.len());
        for function in $set {
            functions.insert(function.parse().expect("invalid function name"));
        }
        capabilities::Targets::Some(functions)
    }};
}

impl TryFrom<CapabilitiesConfig> for capabilities::Capabilities {
    type Error = Error;

    fn try_from(config: CapabilitiesConfig) -> Result<Self, Self::Error> {
        let mut capabilities = Self::default();

        if let Some(experimental) = config.experimental {
            if let Some(allow) = experimental.allow {
                if let Some(set) = allow.array {
                    capabilities = capabilities.with_experimental(process_targets!(set));
                } else {
                    match allow.bool {
                        Some(true) => {
                            capabilities =
                                capabilities.with_experimental(capabilities::Targets::All);
                        }
                        Some(false) => {
                            capabilities =
                                capabilities.with_experimental(capabilities::Targets::None);
                        }
                        None => (),
                    }
                }
            }

            if let Some(deny) = experimental.deny {
                if let Some(set) = deny.array {
                    capabilities = capabilities.without_experimental(process_targets!(set));
                } else {
                    match deny.bool {
                        Some(true) => {
                            capabilities =
                                capabilities.without_experimental(capabilities::Targets::All);
                        }
                        Some(false) => {
                            capabilities =
                                capabilities.without_experimental(capabilities::Targets::None);
                        }
                        None => (),
                    }
                }
            }
        }

        Ok(capabilities
            // Always allow arbitrary quering in embedded mode,
            // There is no use in configuring that here
            .with_arbitrary_query(capabilities::Targets::All)
            .without_arbitrary_query(capabilities::Targets::None))
    }
}
