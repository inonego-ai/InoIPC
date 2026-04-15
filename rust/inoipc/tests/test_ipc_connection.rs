use inoipc::{IpcConnection, IpcResponse, TestServer, TestTransport, frame_send};

// ----------------------------------------------------------------
// Send / Receive
// ----------------------------------------------------------------

#[test]
fn send_receive_round_trip() {
    let transport = TestTransport::new();
    let mut conn = IpcConnection::new(transport);
    conn.send("{\"test\":\"ping\"}").unwrap();
    let result = conn.receive().unwrap();
    assert_eq!(result, "{\"test\":\"ping\"}");
}

#[test]
fn send_ipc_response() {
    let transport = TestTransport::new();
    let mut conn = IpcConnection::new(transport);
    conn.send_response(&IpcResponse::success_msg("ok")).unwrap();
    let result = conn.receive().unwrap();
    assert!(result.contains("\"success\":true"));
    assert!(result.contains("\"message\":\"ok\""));
}

// ----------------------------------------------------------------
// Request
// ----------------------------------------------------------------

#[test]
fn request_parses_response() {
    let mut transport = TestTransport::new();
    // Pre-load server response into the buffer.
    frame_send(&mut transport, "{\"success\":true,\"value\":42}").unwrap();
    let mut conn = IpcConnection::new(transport);
    let response = conn.request("{\"action\":\"get\"}").unwrap();
    assert!(response.is_success());
}

// ----------------------------------------------------------------
// Server integration
// ----------------------------------------------------------------

#[test]
fn server_sends_client_receives() {
    let transport = TestTransport::new();
    let mut server_conn = IpcConnection::new(transport.clone());
    server_conn.send_response(&IpcResponse::success_msg("ready")).unwrap();

    let mut client_conn = IpcConnection::new(transport);
    let response = client_conn.receive().unwrap();
    assert!(response.contains("\"success\":true"));
    assert!(response.contains("\"message\":\"ready\""));
}

#[test]
fn test_server_callback_receives_connection() {
    let mut server = TestServer::new();
    let called = std::sync::Arc::new(std::sync::atomic::AtomicBool::new(false));
    let called_clone = called.clone();

    server.start(move |_conn| {
        called_clone.store(true, std::sync::atomic::Ordering::Relaxed);
    });

    server.accept().unwrap();
    assert!(called.load(std::sync::atomic::Ordering::Relaxed));
}
