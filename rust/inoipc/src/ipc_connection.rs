use std::time::{Duration, Instant};

use crate::{IpcError, IpcResponse, frame_protocol, transport::Transport};

/// Wraps a Transport with FrameProtocol for JSON messaging.
pub struct IpcConnection<T: Transport> {
    transport: T,
}

impl<T: Transport> IpcConnection<T> {
    pub fn new(transport: T) -> Self {
        IpcConnection { transport }
    }

    /// Sends a raw JSON string via FrameProtocol.
    pub fn send(&mut self, json: &str) -> Result<(), IpcError> {
        frame_protocol::send(&mut self.transport, json)
    }

    /// Sends an IpcResponse via FrameProtocol.
    pub fn send_response(&mut self, response: &IpcResponse) -> Result<(), IpcError> {
        frame_protocol::send(&mut self.transport, response.raw_json())
    }

    /// Receives a JSON string via FrameProtocol.
    pub fn receive(&mut self) -> Result<String, IpcError> {
        frame_protocol::receive(&mut self.transport)
    }

    /// Auto-connects if not connected, sends `json`, receives and parses response.
    pub fn request(&mut self, json: &str) -> Result<IpcResponse, IpcError> {
        if !self.transport.is_connected() {
            self.transport.connect()?;
        }
        self.send(json)?;
        let raw = self.receive()?;
        Ok(IpcResponse::parse(&raw))
    }

    /// Retries on IO or ConnectionFailed errors.
    /// Calls `connect()` on each retry attempt.
    /// Returns `IpcResponse` on successful receive regardless of `is_success()`.
    /// Returns `Err(IpcError::Timeout)` when `timeout_ms` is exceeded.
    pub fn request_with_retry(
        &mut self,
        json: &str,
        timeout_ms: u64,
        interval_ms: u64,
    ) -> Result<IpcResponse, IpcError> {
        let deadline = Instant::now() + Duration::from_millis(timeout_ms);
        loop {
            match self.request(json) {
                Ok(resp) => return Ok(resp),
                Err(IpcError::Io(_)) | Err(IpcError::ConnectionFailed(_)) => {
                    if Instant::now() >= deadline {
                        return Err(IpcError::Timeout(
                            format!("failed to connect after {timeout_ms}ms"),
                        ));
                    }
                    std::thread::sleep(Duration::from_millis(interval_ms));
                    self.transport.disconnect();
                }
                Err(e) => return Err(e),
            }
        }
    }
}
