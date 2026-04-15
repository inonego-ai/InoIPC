use std::fmt;

#[derive(Debug)]
pub enum IpcError {
    Io(std::io::Error),
    Json(serde_json::Error),
    Frame(String),
    ConnectionFailed(String),
    Timeout(String),
    ReservedKey(String),
    ServerNotStarted,
}

impl fmt::Display for IpcError {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        match self {
            IpcError::Io(e)                => write!(f, "IO error: {e}"),
            IpcError::Json(e)              => write!(f, "JSON error: {e}"),
            IpcError::Frame(msg)           => write!(f, "Frame error: {msg}"),
            IpcError::ConnectionFailed(msg)=> write!(f, "Connection failed: {msg}"),
            IpcError::Timeout(msg)         => write!(f, "Timeout: {msg}"),
            IpcError::ReservedKey(key)     => write!(f, "Reserved key: '{key}'"),
            IpcError::ServerNotStarted     => write!(f, "Server not started"),
        }
    }
}

impl std::error::Error for IpcError {
    fn source(&self) -> Option<&(dyn std::error::Error + 'static)> {
        match self {
            IpcError::Io(e)   => Some(e),
            IpcError::Json(e) => Some(e),
            _                 => None,
        }
    }
}

impl From<std::io::Error> for IpcError {
    fn from(e: std::io::Error) -> Self {
        IpcError::Io(e)
    }
}

impl From<serde_json::Error> for IpcError {
    fn from(e: serde_json::Error) -> Self {
        IpcError::Json(e)
    }
}
