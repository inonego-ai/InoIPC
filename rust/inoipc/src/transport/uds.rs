use std::io::{Read, Write};
use std::os::unix::net::{UnixListener, UnixStream};
use std::path::PathBuf;
use std::sync::{
    Arc,
    atomic::{AtomicBool, Ordering},
};
use std::thread;

use crate::{IpcConnection, IpcError, transport::Transport};

// ----------------------------------------------------------------
// UdsTransport
// ----------------------------------------------------------------

pub struct UdsTransport {
    path: PathBuf,
    stream: Option<UnixStream>,
}

impl UdsTransport {
    pub fn new(path: impl Into<PathBuf>) -> Self {
        UdsTransport { path: path.into(), stream: None }
    }

    pub(crate) fn from_stream(stream: UnixStream) -> Self {
        UdsTransport { path: PathBuf::new(), stream: Some(stream) }
    }
}

impl Transport for UdsTransport {
    fn connect(&mut self) -> Result<(), IpcError> {
        let s = UnixStream::connect(&self.path).map_err(IpcError::Io)?;
        self.stream = Some(s);
        Ok(())
    }

    fn read(&mut self, buf: &mut [u8]) -> Result<(), IpcError> {
        self.stream
            .as_mut()
            .ok_or_else(|| IpcError::ConnectionFailed("not connected".into()))?
            .read_exact(buf)
            .map_err(IpcError::Io)
    }

    fn write(&mut self, data: &[u8]) -> Result<(), IpcError> {
        self.stream
            .as_mut()
            .ok_or_else(|| IpcError::ConnectionFailed("not connected".into()))?
            .write_all(data)
            .map_err(IpcError::Io)
    }

    fn is_connected(&self) -> bool { self.stream.is_some() }

    fn disconnect(&mut self) {
        if let Some(s) = self.stream.take() {
            let _ = s.shutdown(std::net::Shutdown::Both);
        }
    }
}

// ----------------------------------------------------------------
// UdsServer
// ----------------------------------------------------------------

pub struct UdsServer {
    path: PathBuf,
    stop: Arc<AtomicBool>,
}

impl UdsServer {
    pub fn new(path: impl Into<PathBuf>) -> Self {
        UdsServer { path: path.into(), stop: Arc::new(AtomicBool::new(false)) }
    }

    /// Starts accepting connections. Blocks until `stop()` is called.
    /// Removes the socket file on start; caller must ensure the path is free.
    pub fn start<F>(&self, on_client: F) -> Result<(), IpcError>
    where
        F: Fn(IpcConnection<UdsTransport>) + Send + 'static + Clone,
    {
        let _ = std::fs::remove_file(&self.path);
        let listener = UnixListener::bind(&self.path).map_err(IpcError::Io)?;
        listener.set_nonblocking(true).map_err(IpcError::Io)?;
        self.stop.store(false, Ordering::Relaxed);

        loop {
            if self.stop.load(Ordering::Relaxed) {
                break;
            }
            match listener.accept() {
                Ok((stream, _)) => {
                    let _ = stream.set_nonblocking(false);
                    let on_client = on_client.clone();
                    thread::spawn(move || {
                        let transport = UdsTransport::from_stream(stream);
                        let conn = IpcConnection::new(transport);
                        on_client(conn);
                    });
                }
                Err(e) if e.kind() == std::io::ErrorKind::WouldBlock => {
                    thread::sleep(std::time::Duration::from_millis(1));
                }
                Err(e) => {
                    let _ = std::fs::remove_file(&self.path);
                    return Err(IpcError::Io(e));
                }
            }
        }

        let _ = std::fs::remove_file(&self.path);
        Ok(())
    }

    pub fn stop(&self) {
        self.stop.store(true, Ordering::Relaxed);
    }
}
