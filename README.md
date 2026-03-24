<p align="center">
  <h1 align="center">InoIPC</h1>
  <p align="center">
    .NET IPC Framework
  </p>
  <p align="center">
    <a href="https://opensource.org/licenses/MIT"><img src="https://img.shields.io/badge/License-MIT-yellow.svg" alt="License: MIT"></a>
    <img src="https://img.shields.io/badge/.NET-8.0-purple?logo=dotnet" alt=".NET 8.0">
  </p>
  <p align="center">
    <b>English</b> | <a href="README.ko.md">한국어</a>
  </p>
</p>

---

Length-prefixed frame protocol over TCP, Named Pipe, and Unix Domain Socket. Transport, server, connection, and JSON response — all in one.

## Structure

```
InoIPC/
├── src/InoIPC/
│   ├── Transport/          ITransport, IServer
│   │   ├── Tcp/            TcpTransport, TcpServer
│   │   ├── NamedPipe/      NamedPipeTransport, NamedPipeServer
│   │   ├── Uds/            UdsTransport, UdsServer
│   │   └── Test/           TestTransport, TestServer
│   ├── Protocol/           FrameProtocol
│   ├── Connection/         IpcConnection
│   ├── Models/             IpcResponse, IpcException
│   └── Json/               JsonHelper
└── test/
```

## Installation

```bash
git submodule add https://github.com/inonego/InoIPC.git lib/InoIPC
```

```xml
<ItemGroup>
  <ProjectReference Include="../lib/InoIPC/src/InoIPC/InoIPC.csproj" />
</ItemGroup>
```

## Usage

### Client

```csharp
using var transport = new NamedPipeTransport("my-service");
var conn = new IpcConnection(transport);

IpcResponse response = conn.Request("{\"action\":\"ping\"}");

Console.WriteLine(response.IsSuccess);  // true
Console.WriteLine(response.RawJson);    // {"success":true,"message":"pong"}
```

### Server

```csharp
var server = new NamedPipeServer("my-service");

server.Start(conn =>
{
   string request = conn.Receive();

   conn.Send(IpcResponse.Success("pong"));
});
```

### Both sides use IpcConnection

```csharp
conn.Send(json);                    // send raw JSON
conn.Send(IpcResponse.Success());   // send IpcResponse
conn.Receive();                     // receive raw JSON
conn.Request(json);                 // send + receive + parse
conn.RequestWithRetry(json);        // with retry on failure
```

## Transport

| Type | Transport | Server | Use Case |
|------|-----------|--------|----------|
| TCP | `TcpTransport` | `TcpServer` | Network / remote |
| Named Pipe | `NamedPipeTransport` | `NamedPipeServer` | Local daemon |
| UDS | `UdsTransport` | `UdsServer` | Local (Linux/macOS) |
| Test | `TestTransport` | `TestServer` | Unit testing |

All transports implement `ITransport` (raw `Write`/`Read`). All servers implement `IServer` (`Start`/`Stop`).

### Named Pipe Discovery

```csharp
NamedPipeTransport.Find("myapp-");       // first matching pipe
NamedPipeTransport.FindAll("myapp-");    // all matching pipes
```

## Frame Protocol

Length-prefixed framing: `[4-byte BE uint32 length][UTF-8 body]`

```csharp
FrameProtocol.Send(transport, json);
string response = FrameProtocol.Receive(transport);
```

## IpcResponse

```csharp
IpcResponse.Success()                             // {"success":true}
IpcResponse.Success("Connected")                  // {"success":true,"message":"Connected"}
IpcResponse.Success("port", 8080)                 // {"success":true,"port":8080}
IpcResponse.Success(dict)                         // {"success":true,...}
IpcResponse.Error("TIMEOUT", "timed out")         // {"success":false,"error":{...}}
IpcResponse.Parse(json)                           // parse response JSON
```

## JsonHelper

```csharp
// Read (from JsonElement)
JsonHelper.GetInt(element, fallback)
JsonHelper.GetFloat(element, fallback)
JsonHelper.GetString(element, fallback)
JsonHelper.GetBool(element, fallback)

// Write (to console)
JsonHelper.Write(json, pretty);
JsonHelper.WriteError(json, pretty);
JsonHelper.Prettify(json);
```

## License

[MIT](LICENSE)
