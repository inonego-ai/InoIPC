use std::io::{Read, Write};
use std::net::{SocketAddr, TcpListener, TcpStream, ToSocketAddrs};
use std::sync::{
    Arc,
    atomic::{AtomicBool, Ordering},
};
use std::thread;

use crate::{IpcConnection, IpcError, transport::Transport};

// ----------------------------------------------------------------
// TcpTransport
// ----------------------------------------------------------------

pub struct TcpTransport {
    addr: SocketAddr,
    stream: Option<TcpStream>,
}

impl TcpTransport {
    pub fn new(addr: impl ToSocketAddrs) -> Result<Self, IpcError> {
        let addr = addr
            .to_socket_addrs()
            .map_err(IpcError::Io)?
            .next()
            .ok_or_else(|| IpcError::ConnectionFailed("could not resolve address".into()))?;
        Ok(TcpTransport { addr, stream: None })
    }

    pub(crate) fn from_stream(stream: TcpStream) -> Self {
        let addr = stream.peer_addr().unwrap_or("0.0.0.0:0".parse().unwrap());
        TcpTransport { addr, stream: Some(stream) }
    }
}

impl Transport for TcpTransport {
    fn connect(&mut self) -> Result<(), IpcError> {
        let s = TcpStream::connect(self.addr).map_err(IpcError::Io)?;
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
// TcpServer
// ----------------------------------------------------------------

pub struct TcpServer {
    listener: TcpListener,
    stop: Arc<AtomicBool>,
}

impl TcpServer {
    pub fn new(addr: impl ToSocketAddrs) -> Result<Self, IpcError> {
        let listener = TcpListener::bind(addr).map_err(IpcError::Io)?;
        listener.set_nonblocking(true).map_err(IpcError::Io)?;
        Ok(TcpServer { listener, stop: Arc::new(AtomicBool::new(false)) })
    }

    /// Returns the local address the server is listening on.
    pub fn local_addr(&self) -> Result<SocketAddr, IpcError> {
        self.listener.local_addr().map_err(IpcError::Io)
    }

    /// Starts accepting connections. Blocks until `stop()` is called.
    /// Each connection is handled in a new thread.
    pub fn start<F>(&self, on_client: F) -> Result<(), IpcError>
    where
        F: Fn(IpcConnection<TcpTransport>) + Send + 'static + Clone,
    {
        self.stop.store(false, Ordering::Relaxed);
        loop {
            if self.stop.load(Ordering::Relaxed) {
                break;
            }
            match self.listener.accept() {
                Ok((stream, _)) => {
                    let _ = stream.set_nonblocking(false);
                    let on_client = on_client.clone();
                    thread::spawn(move || {
                        let transport = TcpTransport::from_stream(stream);
                        let conn = IpcConnection::new(transport);
                        on_client(conn);
                    });
                }
                Err(e) if e.kind() == std::io::ErrorKind::WouldBlock => {
                    thread::sleep(std::time::Duration::from_millis(1));
                }
                Err(e) => return Err(IpcError::Io(e)),
            }
        }
        Ok(())
    }

    /// Signals the accept loop to stop. `start()` returns within ~1ms.
    pub fn stop(&self) {
        self.stop.store(true, Ordering::Relaxed);
    }
}
