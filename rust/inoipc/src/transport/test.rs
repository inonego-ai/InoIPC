use std::collections::VecDeque;
use std::sync::{Arc, Condvar, Mutex};

use crate::{IpcConnection, IpcError, transport::Transport};

// ----------------------------------------------------------------
// TestTransport — in-memory FIFO byte buffer.
// Write appends; read consumes from the front (read-exact semantics,
// blocks on Condvar until enough bytes are available).
// Use TestTransport::new() for loopback (write then read same buffer).
// Use TestTransport::pair() for a bidirectional client/server pair.
// ----------------------------------------------------------------

type Buf = Arc<(Mutex<VecDeque<u8>>, Condvar)>;

pub struct TestTransport {
    pub(crate) rx: Buf,
    pub(crate) tx: Buf,
}

impl Clone for TestTransport {
    /// Clones the transport, sharing the same underlying byte buffers.
    /// Useful for passing the same buffer to multiple IpcConnections.
    fn clone(&self) -> Self {
        TestTransport { rx: Arc::clone(&self.rx), tx: Arc::clone(&self.tx) }
    }
}

impl TestTransport {
    /// Single loopback buffer: write and read on the same transport.
    pub fn new() -> Self {
        let buf: Buf = Arc::new((Mutex::new(VecDeque::new()), Condvar::new()));
        TestTransport { rx: Arc::clone(&buf), tx: Arc::clone(&buf) }
    }

    /// Bidirectional pair: writes to A are readable from B and vice versa.
    pub fn pair() -> (TestTransport, TestTransport) {
        let ab: Buf = Arc::new((Mutex::new(VecDeque::new()), Condvar::new()));
        let ba: Buf = Arc::new((Mutex::new(VecDeque::new()), Condvar::new()));
        let a = TestTransport { rx: Arc::clone(&ab), tx: Arc::clone(&ba) };
        let b = TestTransport { rx: Arc::clone(&ba), tx: Arc::clone(&ab) };
        (a, b)
    }
}

impl Default for TestTransport {
    fn default() -> Self { Self::new() }
}

impl Transport for TestTransport {
    fn connect(&mut self) -> Result<(), IpcError> { Ok(()) }

    fn read(&mut self, buf: &mut [u8]) -> Result<(), IpcError> {
        let (lock, cvar) = &*self.rx;
        let mut queue = lock.lock().unwrap();
        let need = buf.len();
        loop {
            if queue.len() >= need {
                for byte in buf.iter_mut() {
                    *byte = queue.pop_front().unwrap();
                }
                return Ok(());
            }
            queue = cvar.wait(queue).unwrap();
        }
    }

    fn write(&mut self, data: &[u8]) -> Result<(), IpcError> {
        let (lock, cvar) = &*self.tx;
        let mut queue = lock.lock().unwrap();
        queue.extend(data.iter().copied());
        cvar.notify_all();
        Ok(())
    }

    fn is_connected(&self) -> bool { true }

    fn disconnect(&mut self) {}
}

// ----------------------------------------------------------------
// TestServer — simulates a server for unit tests.
// Does NOT block; use accept() to manually trigger a client.
// ----------------------------------------------------------------

pub struct TestServer {
    handler: Option<Box<dyn Fn(IpcConnection<TestTransport>) + Send + Sync + 'static>>,
    running: bool,
}

impl TestServer {
    pub fn new() -> Self {
        TestServer { handler: None, running: false }
    }

    pub fn start<F>(&mut self, on_client: F)
    where
        F: Fn(IpcConnection<TestTransport>) + Send + Sync + 'static,
    {
        self.handler = Some(Box::new(on_client));
        self.running = true;
    }

    pub fn stop(&mut self) {
        self.running = false;
        self.handler = None;
    }

    /// Simulates one client connection. Runs the handler synchronously.
    /// Returns the transport for post-call assertion.
    pub fn accept(&self) -> Result<TestTransport, IpcError> {
        if !self.running || self.handler.is_none() {
            return Err(IpcError::ServerNotStarted);
        }
        let transport = TestTransport::new();
        let conn_transport = TestTransport { rx: Arc::clone(&transport.rx), tx: Arc::clone(&transport.tx) };
        let conn = IpcConnection::new(conn_transport);
        (self.handler.as_ref().unwrap())(conn);
        Ok(transport)
    }
}

impl Default for TestServer {
    fn default() -> Self { Self::new() }
}
