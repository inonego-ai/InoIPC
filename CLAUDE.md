# InoIPC — Canonical Feature Contract

This file is the source of truth for both language implementations.
When working on either `csharp/` or `rust/`, this contract defines
what each implementation must support.

## Feature Set

### 1. Frame Protocol

- **Encoding** — `[4-byte big-endian uint32 length][UTF-8 body]`
- **Send** — encodes a string as UTF-8, writes length prefix + body
- **Receive** — reads the 4-byte header, then reads exactly `length` bytes, decodes UTF-8
- **Read-exact semantics** — `Receive` blocks until the full frame arrives or returns an error
- **Multiple frames** — frames are independent; multiple frames can be sent and received in sequence on the same transport

### 2. Transport Abstraction

All transports expose the same interface:

| Operation | C# | Rust |
|-----------|-----|------|
| Connect | `Connect()` | `connect() -> Result<(), IpcError>` |
| Write | `Write(byte[], offset, count)` | `write(data: &[u8]) -> Result<(), IpcError>` |
| Read (exact) | `Read(buf, offset, count)` | `read(buf: &mut [u8]) -> Result<(), IpcError>` |
| IsConnected | `IsConnected` (property) | `is_connected() -> bool` |
| Disconnect | `Dispose()` | `disconnect()` |

Implementations: **TCP**, **Named Pipe** (Windows-only), **UDS** (Unix-only), **TestTransport** (in-memory).

### 3. IpcConnection

Wraps a transport with FrameProtocol for JSON messaging:

| Method | Behaviour |
|--------|-----------|
| `Send(json)` | Encodes and sends a JSON string via FrameProtocol |
| `Send(IpcResponse)` | Sends the raw JSON of a response |
| `Receive()` | Reads and returns a JSON string via FrameProtocol |
| `Request(json)` | Auto-connects if not connected, sends, receives, parses as IpcResponse |
| `RequestWithRetry(json, timeoutMs, intervalMs)` | Retries on IO/connection errors; re-calls Connect on retry |

### 4. IpcResponse

JSON envelope with a mandatory `"success": bool` field.

| Builder | Output |
|---------|--------|
| `Success()` | `{"success":true}` |
| `Success(message)` | `{"success":true,"message":"..."}` |
| `Success(key, value)` | `{"success":true,"<key>":<value>}` |
| `Success(dict)` | `{"success":true,...dict}` |
| `Error(code, message)` | `{"success":false,"error":{"code":"...","message":"..."}}` |
| `Error(code, message, data)` | `{"success":false,"error":{"code":"...","message":"...",...data}}` |

Reserved keys: `"success"` in Success builders; `"code"` and `"message"` in Error data.
Using a reserved key throws / returns `Err`.

Parse:
- `Parse(json)` / `parse(json)` — extracts `success` field; returns `IsSuccess = false` on parse failure.

### 5. Server

Each transport has a server variant that:
- **Accepts** connections in a loop
- **Spawns** a handler per connection (thread in C#/Rust sync; async Task in C# async)
- **Stops** cleanly on `Stop()` / `stop()`
- **Blocks** the calling thread until stopped

C# supports both sync (`Action<IpcConnection>`) and async (`Func<IpcConnection, Task>`) callbacks.
Rust uses a sync `Fn(IpcConnection<T>) + Send + 'static` callback (tokio not required).

---

## Language-Specific Notes

### C# (`csharp/`)

- **Toolchain:** `dotnet test csharp/test/InoIPC.TEST.csproj`
- **Targets:** `net8.0`, `netstandard2.1`
- **JSON:** `System.Text.Json` on .NET 8+; `Newtonsoft.Json` on Standard 2.1
- **Error handling:** throws `IpcException` / `ArgumentException` on invalid input

### Rust (`rust/`)

- **Toolchain:** `cargo test` from `rust/`
- **Edition:** 2024 (rustc 1.85+)
- **Error handling:** returns `Result<T, IpcError>` — never panics on user input
- **NamedPipe:** Windows-only, `#[cfg(windows)]`; uses `windows-sys` crate
- **UDS:** Unix-only, `#[cfg(unix)]`; uses `std::os::unix::net`
- **Stop mechanism:**
  - TCP/UDS: nonblocking accept loop with 1ms sleep + `AtomicBool` flag
  - NamedPipe: `CreateEvent` + `WaitForMultipleObjects`
- **Distribution:** git submodule + path dep for v1; crates.io planned post-stabilization

---

## Adding Features

When you add a feature to one implementation:
1. Update this file's Feature Set table
2. Add the equivalent to the other implementation
3. Add tests in both `csharp/test/` and `rust/inoipc/tests/`
