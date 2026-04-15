mod error;
mod frame_protocol;
mod ipc_connection;
mod ipc_response;
pub mod transport;

pub use error::IpcError;
pub use frame_protocol::{receive as frame_receive, send as frame_send};
pub use ipc_connection::IpcConnection;
pub use ipc_response::IpcResponse;
pub use transport::{TcpServer, TcpTransport, TestServer, TestTransport, Transport};

#[cfg(unix)]
pub use transport::{UdsServer, UdsTransport};

#[cfg(windows)]
pub use transport::{NamedPipeServer, NamedPipeTransport};
