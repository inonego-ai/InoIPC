//! Windows Named Pipe transport and server.
//! Only compiled on Windows (`#[cfg(windows)]` in transport/mod.rs).

use std::ffi::OsStr;
use std::io::{Read, Write};
use std::os::windows::io::{FromRawHandle, IntoRawHandle};
use std::os::windows::ffi::OsStrExt;
use std::sync::{
    Arc,
    atomic::{AtomicBool, Ordering},
};
use std::thread;

use windows_sys::Win32::Foundation::{
    CloseHandle, HANDLE, INVALID_HANDLE_VALUE, FALSE, WAIT_OBJECT_0,
};
use windows_sys::Win32::Storage::FileSystem::{
    CreateFileW, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL,
};

// GENERIC_READ / GENERIC_WRITE are access mask constants not exported
// from Win32::Storage::FileSystem in windows-sys 0.52 — define as const.
const GENERIC_READ:  u32 = 0x80000000;
const GENERIC_WRITE: u32 = 0x40000000;
use windows_sys::Win32::System::IO::OVERLAPPED;
use windows_sys::Win32::System::Pipes::{
    ConnectNamedPipe, CreateNamedPipeW, DisconnectNamedPipe,
    PIPE_TYPE_BYTE, PIPE_READMODE_BYTE, PIPE_WAIT,
    PIPE_UNLIMITED_INSTANCES,
};
use windows_sys::Win32::System::Threading::{
    CreateEventW, SetEvent, WaitForMultipleObjects, INFINITE,
};

use crate::{IpcConnection, IpcError, transport::Transport};

// FILE_FLAG_OVERLAPPED is 0x40000000 — defined in Win32::Storage::FileSystem
// but only with Win32_System_IO feature. Define as const to avoid import dance.
const FILE_FLAG_OVERLAPPED: u32 = 0x40000000;
const PIPE_ACCESS_DUPLEX:    u32 = 0x00000003;

fn to_wide(s: &str) -> Vec<u16> {
    OsStr::new(s).encode_wide().chain(std::iter::once(0)).collect()
}

fn pipe_path(name: &str) -> String {
    format!(r"\\.\pipe\{name}")
}

// ----------------------------------------------------------------
// NamedPipeTransport
// ----------------------------------------------------------------

pub struct NamedPipeTransport {
    name: Option<String>,
    handle: HANDLE,
}

unsafe impl Send for NamedPipeTransport {}

impl NamedPipeTransport {
    pub fn new(name: &str) -> Self {
        NamedPipeTransport { name: Some(name.to_string()), handle: INVALID_HANDLE_VALUE }
    }

    pub(crate) fn from_handle(handle: HANDLE) -> Self {
        NamedPipeTransport { name: None, handle }
    }

    /// Returns true if a named pipe server with this name is running.
    pub fn exists(name: &str) -> bool {
        let path = to_wide(&pipe_path(name));
        unsafe {
            let h = CreateFileW(
                path.as_ptr(),
                GENERIC_READ | GENERIC_WRITE,
                0,
                std::ptr::null(),
                OPEN_EXISTING,
                FILE_ATTRIBUTE_NORMAL,
                0,
            );
            if h != INVALID_HANDLE_VALUE {
                CloseHandle(h);
                true
            } else {
                false
            }
        }
    }
}

impl Transport for NamedPipeTransport {
    fn connect(&mut self) -> Result<(), IpcError> {
        let name = self.name.as_ref()
            .ok_or_else(|| IpcError::ConnectionFailed("no pipe name".into()))?;
        let path = to_wide(&pipe_path(name));
        let h = unsafe {
            CreateFileW(
                path.as_ptr(),
                GENERIC_READ | GENERIC_WRITE,
                0,
                std::ptr::null(),
                OPEN_EXISTING,
                FILE_ATTRIBUTE_NORMAL,
                0,
            )
        };
        if h == INVALID_HANDLE_VALUE {
            return Err(IpcError::ConnectionFailed(
                format!("CreateFileW failed for pipe '{name}'"),
            ));
        }
        if self.handle != INVALID_HANDLE_VALUE {
            unsafe { CloseHandle(self.handle); }
        }
        self.handle = h;
        Ok(())
    }

    fn read(&mut self, buf: &mut [u8]) -> Result<(), IpcError> {
        // Wrap HANDLE as File for read_exact semantics, then recover the handle.
        let mut file = unsafe { std::fs::File::from_raw_handle(self.handle as _) };
        let result = file.read_exact(buf).map_err(IpcError::Io);
        self.handle = file.into_raw_handle() as HANDLE;
        result
    }

    fn write(&mut self, data: &[u8]) -> Result<(), IpcError> {
        let mut file = unsafe { std::fs::File::from_raw_handle(self.handle as _) };
        let result = file.write_all(data).map_err(IpcError::Io);
        self.handle = file.into_raw_handle() as HANDLE;
        result
    }

    fn is_connected(&self) -> bool { self.handle != INVALID_HANDLE_VALUE }

    fn disconnect(&mut self) {
        if self.handle != INVALID_HANDLE_VALUE {
            unsafe {
                DisconnectNamedPipe(self.handle);
                CloseHandle(self.handle);
            }
            self.handle = INVALID_HANDLE_VALUE;
        }
    }
}

impl Drop for NamedPipeTransport {
    fn drop(&mut self) { self.disconnect(); }
}

// ----------------------------------------------------------------
// NamedPipeServer
// ----------------------------------------------------------------

pub struct NamedPipeServer {
    name: String,
    stop: Arc<AtomicBool>,
}

unsafe impl Send for NamedPipeServer {}
unsafe impl Sync for NamedPipeServer {}

impl NamedPipeServer {
    pub fn new(name: &str) -> Self {
        NamedPipeServer { name: name.to_string(), stop: Arc::new(AtomicBool::new(false)) }
    }

    /// Starts accepting connections. Blocks until `stop()` is called.
    /// Uses WaitForMultipleObjects so stop() unblocks the accept loop immediately.
    pub fn start<F>(&self, on_client: F) -> Result<(), IpcError>
    where
        F: Fn(IpcConnection<NamedPipeTransport>) + Send + 'static + Clone,
    {
        self.stop.store(false, Ordering::Relaxed);

        // Create a manual-reset event that gets signalled when stop() is called.
        let stop_event: HANDLE = unsafe {
            CreateEventW(std::ptr::null(), FALSE as _, FALSE as _, std::ptr::null())
        };

        // Background thread watches the AtomicBool and fires the event.
        let stop_flag = Arc::clone(&self.stop);
        thread::spawn(move || {
            while !stop_flag.load(Ordering::Relaxed) {
                thread::sleep(std::time::Duration::from_millis(1));
            }
            unsafe { SetEvent(stop_event); }
        });

        loop {
            if self.stop.load(Ordering::Relaxed) { break; }

            let path = to_wide(&pipe_path(&self.name));
            let pipe = unsafe {
                CreateNamedPipeW(
                    path.as_ptr(),
                    PIPE_ACCESS_DUPLEX | FILE_FLAG_OVERLAPPED,
                    PIPE_TYPE_BYTE | PIPE_READMODE_BYTE | PIPE_WAIT,
                    PIPE_UNLIMITED_INSTANCES,
                    65536,
                    65536,
                    0,
                    std::ptr::null(),
                )
            };
            if pipe == INVALID_HANDLE_VALUE {
                unsafe { CloseHandle(stop_event); }
                return Err(IpcError::ConnectionFailed("CreateNamedPipeW failed".into()));
            }

            // Overlapped ConnectNamedPipe so WaitForMultipleObjects can interrupt it.
            let connect_event: HANDLE = unsafe {
                CreateEventW(std::ptr::null(), FALSE as _, FALSE as _, std::ptr::null())
            };
            let mut ov = unsafe { std::mem::zeroed::<OVERLAPPED>() };
            ov.hEvent = connect_event;

            let _ = unsafe { ConnectNamedPipe(pipe, &mut ov) };

            let handles = [connect_event, stop_event];
            let result = unsafe {
                WaitForMultipleObjects(2, handles.as_ptr(), FALSE as _, INFINITE)
            };
            unsafe { CloseHandle(connect_event); }

            if result != WAIT_OBJECT_0 {
                // stop_event fired — clean up pipe and exit.
                unsafe { CloseHandle(pipe); }
                break;
            }

            let on_client = on_client.clone();
            thread::spawn(move || {
                let transport = NamedPipeTransport::from_handle(pipe);
                let conn = IpcConnection::new(transport);
                on_client(conn);
            });
        }

        unsafe { CloseHandle(stop_event); }
        Ok(())
    }

    pub fn stop(&self) {
        self.stop.store(true, Ordering::Relaxed);
    }
}
