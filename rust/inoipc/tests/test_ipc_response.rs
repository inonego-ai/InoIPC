use std::collections::HashMap;

use serde_json::json;

use inoipc::IpcResponse;

// ----------------------------------------------------------------
// Success builders
// ----------------------------------------------------------------

#[test]
fn success_empty() {
    let r = IpcResponse::success();
    assert!(r.is_success());
    assert!(r.raw_json().contains("\"success\":true"));
}

#[test]
fn success_message() {
    let r = IpcResponse::success_msg("Connected");
    assert!(r.is_success());
    assert!(r.raw_json().contains("\"message\":\"Connected\""));
}

#[test]
fn success_kv() {
    let r = IpcResponse::success_kv("port", json!(56400)).unwrap();
    assert!(r.is_success());
    assert!(r.raw_json().contains("\"port\":56400"));
}

#[test]
fn success_map() {
    let mut data = HashMap::new();
    data.insert("port".to_string(), json!(56400));
    data.insert("host".to_string(), json!("localhost"));
    let r = IpcResponse::success_map(&data).unwrap();
    assert!(r.is_success());
    assert!(r.raw_json().contains("\"port\":56400"));
    assert!(r.raw_json().contains("\"host\":\"localhost\""));
}

#[test]
fn success_kv_reserved_key_errors() {
    let result = IpcResponse::success_kv("success", json!(true));
    assert!(result.is_err());
}

#[test]
fn success_map_reserved_key_errors() {
    let mut data = HashMap::new();
    data.insert("success".to_string(), json!(true));
    let result = IpcResponse::success_map(&data);
    assert!(result.is_err());
}

// ----------------------------------------------------------------
// Error builders
// ----------------------------------------------------------------

#[test]
fn error_code_message() {
    let r = IpcResponse::error("TIMEOUT", "timed out");
    assert!(!r.is_success());
    assert!(r.raw_json().contains("\"code\":\"TIMEOUT\""));
    assert!(r.raw_json().contains("\"message\":\"timed out\""));
}

#[test]
fn error_with_data() {
    let mut data = HashMap::new();
    data.insert("elapsed".to_string(), json!(5000));
    let r = IpcResponse::error_with("TIMEOUT", "timed out", &data).unwrap();
    assert!(!r.is_success());
    assert!(r.raw_json().contains("\"elapsed\":5000"));
}

#[test]
fn error_data_reserved_key_errors() {
    let mut data = HashMap::new();
    data.insert("code".to_string(), json!("override"));
    let result = IpcResponse::error_with("X", "x", &data);
    assert!(result.is_err());
}

// ----------------------------------------------------------------
// Parse
// ----------------------------------------------------------------

#[test]
fn parse_success() {
    let r = IpcResponse::parse("{\"success\":true,\"port\":56400}");
    assert!(r.is_success());
}

#[test]
fn parse_error() {
    let r = IpcResponse::parse("{\"success\":false,\"error\":{\"code\":\"X\"}}");
    assert!(!r.is_success());
}

#[test]
fn parse_invalid_json() {
    let r = IpcResponse::parse("not json");
    assert!(!r.is_success());
}
