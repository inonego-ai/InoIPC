use crate::{IpcError, transport::Transport};

/// Encodes `message` as UTF-8 and writes `[4-byte BE u32 length][body]`.
pub fn send(transport: &mut dyn Transport, message: &str) -> Result<(), IpcError> {
    let body = message.as_bytes();
    let len = body.len() as u32;
    let header = [
        ((len >> 24) & 0xFF) as u8,
        ((len >> 16) & 0xFF) as u8,
        ((len >>  8) & 0xFF) as u8,
        ( len        & 0xFF) as u8,
    ];
    transport.write(&header)?;
    transport.write(body)?;
    Ok(())
}

/// Reads a length-prefixed frame and decodes it as UTF-8.
/// Returns `IpcError::Frame` on truncated read or invalid UTF-8.
pub fn receive(transport: &mut dyn Transport) -> Result<String, IpcError> {
    let mut header = [0u8; 4];
    transport.read(&mut header)?;

    let len = ((header[0] as u32) << 24)
            | ((header[1] as u32) << 16)
            | ((header[2] as u32) <<  8)
            |  (header[3] as u32);

    let mut body = vec![0u8; len as usize];
    transport.read(&mut body)?;

    String::from_utf8(body)
        .map_err(|e| IpcError::Frame(format!("invalid UTF-8: {e}")))
}
