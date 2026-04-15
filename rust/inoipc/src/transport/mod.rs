use crate::IpcError;

/// Raw byte stream abstraction. Read uses read-exact semantics:
/// `read()` blocks until exactly `buf.len()` bytes are filled or errors.
pub trait Transport: Send {
    fn connect(&mut self) -> Result<(), IpcError>;
    /// Fills `buf` completely (read-exact). Blocks until all bytes arrive.
    fn read(&mut self, buf: &mut [u8]) -> Result<(), IpcError>;
    fn write(&mut self, data: &[u8]) -> Result<(), IpcError>;
    fn is_connected(&self) -> bool;
    /// Best-effort close. Ignores errors.
    fn disconnect(&mut self);
}

pub mod tcp;
pub mod test;

#[cfg(unix)]
pub mod uds;

#[cfg(windows)]
pub mod named_pipe;

pub use tcp::{TcpServer, TcpTransport};
pub use test::{TestServer, TestTransport};

#[cfg(unix)]
pub use uds::{UdsServer, UdsTransport};

#[cfg(windows)]
pub use named_pipe::{NamedPipeServer, NamedPipeTransport};
