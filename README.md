<p align="center">
  <h1 align="center">InoIPC</h1>
  <p align="center">
    IPC Framework — Frame Protocol + JSON Messaging
  </p>
  <p align="center">
    <a href="https://opensource.org/licenses/MIT"><img src="https://img.shields.io/badge/License-MIT-yellow.svg" alt="License: MIT"></a>
    <img src="https://img.shields.io/badge/.NET-8.0%20%7C%20Standard%202.1-purple?logo=dotnet" alt=".NET 8.0 | Standard 2.1">
    <img src="https://img.shields.io/badge/Rust-2024-orange?logo=rust" alt="Rust 2024">
  </p>
  <p align="center">
    <b>English</b> | <a href="README.ko.md">한국어</a>
  </p>
</p>

---

Length-prefixed frame protocol over TCP, Named Pipe, and Unix Domain Socket.
Transport, server, connection, and JSON response — in C# and Rust.

Both implementations share the same protocol and feature set.
See [CLAUDE.md](CLAUDE.md) for the canonical feature contract.

## Implementations

| Language | Location | Test |
|----------|----------|------|
| C# (.NET 8 / Standard 2.1) | [`csharp/`](csharp/) | `dotnet test csharp/test/InoIPC.TEST.csproj` |
| Rust (2024 edition) | [`rust/`](rust/) | `cargo test` in `rust/` |

## Repo Structure

```
InoIPC/
├── CLAUDE.md
├── csharp/
│   ├── src/
│   │   ├── Transport/          ITransport, IServer
│   │   │   ├── Tcp/            TcpTransport, TcpServer
│   │   │   ├── NamedPipe/      NamedPipeTransport, NamedPipeServer
│   │   │   ├── Uds/            UdsTransport, UdsServer
│   │   │   └── Test/           TestTransport, TestServer
│   │   ├── Protocol/           FrameProtocol
│   │   ├── Connection/         IpcConnection
│   │   ├── Models/             IpcResponse, IpcException
│   │   └── Json/               JsonHelper
│   └── test/
└── rust/
    └── inoipc/
        ├── src/
        │   ├── lib.rs
        │   ├── error.rs            IpcError
        │   ├── frame_protocol.rs   frame_send / frame_receive
        │   ├── ipc_connection.rs   IpcConnection<T>
        │   ├── ipc_response.rs     IpcResponse
        │   └── transport/
        │       ├── mod.rs          Transport trait
        │       ├── tcp.rs          TcpTransport, TcpServer
        │       ├── uds.rs          UdsTransport, UdsServer  [unix]
        │       ├── named_pipe.rs   NamedPipeTransport, NamedPipeServer  [windows]
        │       └── test.rs         TestTransport, TestServer
        └── tests/
            ├── test_frame_protocol.rs
            ├── test_ipc_connection.rs
            └── test_ipc_response.rs
```

## C# — Installation

```bash
git submodule add https://github.com/inonego-ai/InoIPC.git lib/InoIPC
```

```xml
<ItemGroup>
  <ProjectReference Include="../lib/InoIPC/csharp/src/InoIPC.csproj" />
</ItemGroup>
```

## Rust — Installation

```bash
git submodule add https://github.com/inonego-ai/InoIPC.git lib/InoIPC
```

```toml
[dependencies]
inoipc = { path = "lib/InoIPC/rust/inoipc" }
```

## Frame Protocol

4-byte big-endian length prefix + UTF-8 body. Identical on both sides — a C# server
and a Rust client speak the same protocol.

```csharp
// C#
FrameProtocol.Send(transport, "{\"action\":\"ping\"}");
string msg = FrameProtocol.Receive(transport);
```

```rust
// Rust
frame_send(&mut transport, "{\"action\":\"ping\"}")?;
let msg = frame_receive(&mut transport)?;
```

## IpcConnection

```csharp
// C# — Client
using var transport = new TcpTransport("127.0.0.1", 9000);
var conn = new IpcConnection(transport);
IpcResponse response = conn.Request("{\"action\":\"ping\"}");

Console.WriteLine(response.IsSuccess);  // true
Console.WriteLine(response.RawJson);    // {"success":true,"message":"pong"}
```

```rust
// Rust — Client
let mut transport = TcpTransport::new("127.0.0.1:9000")?;
transport.connect()?;
let mut conn = IpcConnection::new(transport);
let response = conn.request("{\"action\":\"ping\"}")?;

println!("{}", response.is_success());  // true
println!("{}", response.raw_json());    // {"success":true,"message":"pong"}
```

## IpcResponse

```csharp
// C#
IpcResponse.Success()                            // {"success":true}
IpcResponse.Success("pong")                      // {"success":true,"message":"pong"}
IpcResponse.Success("port", 9000)               // {"success":true,"port":9000}
IpcResponse.Error("TIMEOUT", "timed out")       // {"success":false,"error":{...}}
IpcResponse.Parse(json)                          // parses "success" field
```

```rust
// Rust
IpcResponse::success()                           // {"success":true}
IpcResponse::success_msg("pong")                 // {"success":true,"message":"pong"}
IpcResponse::success_kv("port", json!(9000))?    // {"success":true,"port":9000}
IpcResponse::error("TIMEOUT", "timed out")      // {"success":false,"error":{...}}
IpcResponse::parse(json)                         // parses "success" field
```

## Server

```csharp
// C# — synchronous
var server = new TcpServer("127.0.0.1", 9000);
server.Start(conn =>
{
    string request = conn.Receive();
    conn.Send(IpcResponse.Success("pong"));
});

// C# — asynchronous
server.Start(async conn =>
{
    string request = conn.Receive();
    string result  = await ProcessAsync(request);
    conn.Send(IpcResponse.Success(result));
});
```

```rust
// Rust
let server = TcpServer::new("127.0.0.1:9000")?;
std::thread::spawn(move || {
    server.start(|mut conn| {
        let request = conn.receive().unwrap();
        conn.send_response(&IpcResponse::success_msg("pong")).unwrap();
    }).unwrap();
});
// server.stop() to shut down
```

## Transports

| Transport | C# | Rust | Platform |
|-----------|-----|------|----------|
| TCP | `TcpTransport` / `TcpServer` | `TcpTransport` / `TcpServer` | All |
| Named Pipe | `NamedPipeTransport` / `NamedPipeServer` | `NamedPipeTransport` / `NamedPipeServer` | Windows |
| Unix Domain Socket | `UdsTransport` / `UdsServer` | `UdsTransport` / `UdsServer` | Unix |
| Test (in-memory) | `TestTransport` / `TestServer` | `TestTransport` / `TestServer` | All |

## Compatibility

| Target | Version |
|--------|---------|
| .NET | 8.0+ |
| .NET Standard | 2.1 (Unity 2021+) |
| Rust edition | 2024 (rustc 1.85+) |

No external dependencies in C#. Rust uses `serde_json` and `windows-sys` (Windows only).

## License

[MIT](LICENSE)
