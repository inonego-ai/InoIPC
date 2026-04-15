use std::collections::HashMap;

use serde_json::{json, Value};

use crate::IpcError;

/// JSON envelope with a mandatory `"success": bool` field.
pub struct IpcResponse {
    raw_json: String,
    is_success: bool,
}

impl IpcResponse {
    // ----------------------------------------------------------------
    // Success builders
    // ----------------------------------------------------------------

    /// `{"success":true}`
    pub fn success() -> Self {
        IpcResponse::from_value(json!({"success": true}))
    }

    /// `{"success":true,"message":"..."}`
    pub fn success_msg(message: &str) -> Self {
        IpcResponse::from_value(json!({"success": true, "message": message}))
    }

    /// `{"success":true,"<key>":<value>}` — errors on reserved key `"success"`.
    pub fn success_kv(key: &str, value: Value) -> Result<Self, IpcError> {
        if key == "success" {
            return Err(IpcError::ReservedKey("success".into()));
        }
        let mut map = serde_json::Map::new();
        map.insert("success".into(), Value::Bool(true));
        map.insert(key.into(), value);
        Ok(IpcResponse::from_value(Value::Object(map)))
    }

    /// `{"success":true,...data}` — errors if data contains `"success"`.
    pub fn success_map(data: &HashMap<String, Value>) -> Result<Self, IpcError> {
        if data.contains_key("success") {
            return Err(IpcError::ReservedKey("success".into()));
        }
        let mut map = serde_json::Map::new();
        map.insert("success".into(), Value::Bool(true));
        for (k, v) in data {
            map.insert(k.clone(), v.clone());
        }
        Ok(IpcResponse::from_value(Value::Object(map)))
    }

    // ----------------------------------------------------------------
    // Error builders
    // ----------------------------------------------------------------

    /// `{"success":false,"error":{"code":"...","message":"..."}}`
    pub fn error(code: &str, message: &str) -> Self {
        IpcResponse::from_value(json!({
            "success": false,
            "error": { "code": code, "message": message }
        }))
    }

    /// Extended error with extra data. Errors on reserved keys `"code"` / `"message"`.
    pub fn error_with(
        code: &str,
        message: &str,
        data: &HashMap<String, Value>,
    ) -> Result<Self, IpcError> {
        for key in ["code", "message"] {
            if data.contains_key(key) {
                return Err(IpcError::ReservedKey(key.into()));
            }
        }
        let mut error_obj = serde_json::Map::new();
        error_obj.insert("code".into(), Value::String(code.into()));
        error_obj.insert("message".into(), Value::String(message.into()));
        for (k, v) in data {
            error_obj.insert(k.clone(), v.clone());
        }
        Ok(IpcResponse::from_value(json!({
            "success": false,
            "error": Value::Object(error_obj)
        })))
    }

    // ----------------------------------------------------------------
    // Parse
    // ----------------------------------------------------------------

    /// Parses a JSON string. `is_success()` returns false on parse failure.
    pub fn parse(json: &str) -> Self {
        let is_success = serde_json::from_str::<Value>(json)
            .ok()
            .and_then(|v| v.get("success")?.as_bool())
            .unwrap_or(false);
        IpcResponse { raw_json: json.to_string(), is_success }
    }

    // ----------------------------------------------------------------
    // Accessors
    // ----------------------------------------------------------------

    pub fn is_success(&self) -> bool { self.is_success }

    pub fn raw_json(&self) -> &str { &self.raw_json }

    pub fn get_string(&self, key: &str) -> Option<String> {
        let v: Value = serde_json::from_str(&self.raw_json).ok()?;
        v.get(key)?.as_str().map(|s| s.to_string())
    }

    pub fn get_int(&self, key: &str) -> Option<i64> {
        let v: Value = serde_json::from_str(&self.raw_json).ok()?;
        let field = v.get(key)?;
        if let Some(n) = field.as_i64() { return Some(n); }
        field.as_str()?.parse().ok()
    }

    pub fn get_float(&self, key: &str) -> Option<f64> {
        let v: Value = serde_json::from_str(&self.raw_json).ok()?;
        let field = v.get(key)?;
        if let Some(n) = field.as_f64() { return Some(n); }
        field.as_str()?.parse().ok()
    }

    pub fn get_bool(&self, key: &str) -> Option<bool> {
        let v: Value = serde_json::from_str(&self.raw_json).ok()?;
        let field = v.get(key)?;
        if let Some(b) = field.as_bool() { return Some(b); }
        match field.as_str()?.to_lowercase().as_str() {
            "true"  => Some(true),
            "false" => Some(false),
            _       => None,
        }
    }

    // ----------------------------------------------------------------
    // Internal
    // ----------------------------------------------------------------

    fn from_value(v: Value) -> Self {
        let is_success = v.get("success").and_then(|s| s.as_bool()).unwrap_or(false);
        IpcResponse { raw_json: v.to_string(), is_success }
    }
}
