<p align="center">
  <h1 align="center">InoIPC</h1>
  <p align="center">
    IPC 프레임워크 — 프레임 프로토콜 + JSON 메시징
  </p>
  <p align="center">
    <a href="https://opensource.org/licenses/MIT"><img src="https://img.shields.io/badge/License-MIT-yellow.svg" alt="License: MIT"></a>
    <img src="https://img.shields.io/badge/.NET-8.0%20%7C%20Standard%202.1-purple?logo=dotnet" alt=".NET 8.0 | Standard 2.1">
    <img src="https://img.shields.io/badge/Rust-2024-orange?logo=rust" alt="Rust 2024">
  </p>
  <p align="center">
    <a href="README.md">English</a> | <b>한국어</b>
  </p>
</p>

---

TCP, Named Pipe, Unix Domain Socket 위에서 동작하는 길이 접두사 프레임 프로토콜.
트랜스포트, 서버, 커넥션, JSON 응답 — C#과 Rust 두 가지 구현체 제공.

두 구현체가 지원해야 할 기능 명세는 [CLAUDE.md](CLAUDE.md)를 참고.

## 구현체

| 언어 | 위치 | 테스트 |
|------|------|--------|
| C# (.NET 8 / Standard 2.1) | [`csharp/`](csharp/) | `dotnet test csharp/test/InoIPC.TEST.csproj` |
| Rust (2024 에디션) | [`rust/`](rust/) | `cargo test` (`rust/` 에서) |

## 레포 구조

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
        │       ├── mod.rs          Transport 트레이트
        │       ├── tcp.rs          TcpTransport, TcpServer
        │       ├── uds.rs          UdsTransport, UdsServer  [unix]
        │       ├── named_pipe.rs   NamedPipeTransport, NamedPipeServer  [windows]
        │       └── test.rs         TestTransport, TestServer
        └── tests/
            ├── test_frame_protocol.rs
            ├── test_ipc_connection.rs
            └── test_ipc_response.rs
```

## C# — 설치

```bash
git submodule add https://github.com/inonego-ai/InoIPC.git lib/InoIPC
```

```xml
<ItemGroup>
  <ProjectReference Include="../lib/InoIPC/csharp/src/InoIPC.csproj" />
</ItemGroup>
```

## Rust — 설치

```bash
git submodule add https://github.com/inonego-ai/InoIPC.git lib/InoIPC
```

```toml
[dependencies]
inoipc = { path = "lib/InoIPC/rust/inoipc" }
```

## 프레임 프로토콜

4바이트 빅엔디안 길이 + UTF-8 본문. 양쪽이 동일한 포맷을 사용하므로 C# 서버 ↔ Rust 클라이언트가 그대로 통신 가능.

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
// C# — 클라이언트
using var transport = new TcpTransport("127.0.0.1", 9000);
var conn = new IpcConnection(transport);
IpcResponse response = conn.Request("{\"action\":\"ping\"}");

Console.WriteLine(response.IsSuccess);  // true
Console.WriteLine(response.RawJson);    // {"success":true,"message":"pong"}
```

```rust
// Rust — 클라이언트
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
IpcResponse.Success()                       // {"success":true}
IpcResponse.Success("pong")                 // {"success":true,"message":"pong"}
IpcResponse.Success("port", 9000)          // {"success":true,"port":9000}
IpcResponse.Error("TIMEOUT", "timed out")  // {"success":false,"error":{...}}
IpcResponse.Parse(json)                     // "success" 필드 파싱
```

```rust
// Rust
IpcResponse::success()                      // {"success":true}
IpcResponse::success_msg("pong")            // {"success":true,"message":"pong"}
IpcResponse::success_kv("port", json!(9000))?  // {"success":true,"port":9000}
IpcResponse::error("TIMEOUT", "timed out") // {"success":false,"error":{...}}
IpcResponse::parse(json)                    // "success" 필드 파싱
```

## 서버

```csharp
// C# — 동기
var server = new TcpServer("127.0.0.1", 9000);
server.Start(conn =>
{
    string request = conn.Receive();
    conn.Send(IpcResponse.Success("pong"));
});

// C# — 비동기
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
// server.stop() 으로 종료
```

## 트랜스포트

| 트랜스포트 | C# | Rust | 플랫폼 |
|-----------|-----|------|--------|
| TCP | `TcpTransport` / `TcpServer` | `TcpTransport` / `TcpServer` | 전체 |
| Named Pipe | `NamedPipeTransport` / `NamedPipeServer` | `NamedPipeTransport` / `NamedPipeServer` | Windows |
| Unix Domain Socket | `UdsTransport` / `UdsServer` | `UdsTransport` / `UdsServer` | Unix |
| 테스트 (인메모리) | `TestTransport` / `TestServer` | `TestTransport` / `TestServer` | 전체 |

## 호환성

| 항목 | 버전 |
|------|------|
| .NET | 8.0+ |
| .NET Standard | 2.1 (Unity 2021+) |
| Rust edition | 2024 (rustc 1.85+) |

C#은 외부 의존성 없음. Rust는 `serde_json`과 `windows-sys` (Windows 전용) 사용.

## 라이선스

[MIT](LICENSE)
