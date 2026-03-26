<p align="center">
  <h1 align="center">InoIPC</h1>
  <p align="center">
    .NET IPC 프레임워크
  </p>
  <p align="center">
    <a href="https://opensource.org/licenses/MIT"><img src="https://img.shields.io/badge/License-MIT-yellow.svg" alt="License: MIT"></a>
    <img src="https://img.shields.io/badge/.NET-8.0%20%7C%20Standard%202.1-purple?logo=dotnet" alt=".NET 8.0 | Standard 2.1">
  </p>
  <p align="center">
    <a href="README.md">English</a> | <b>한국어</b>
  </p>
</p>

---

길이 접두사 프레임 프로토콜 기반 IPC. TCP, Named Pipe, Unix Domain Socket 지원. Transport, 서버, 커넥션, JSON 응답 — 올인원.

## 구조

```
InoIPC/
├── src/
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

## 설치

```bash
git submodule add https://github.com/inonego/InoIPC.git lib/InoIPC
```

```xml
<ItemGroup>
  <ProjectReference Include="../lib/InoIPC/src/InoIPC.csproj" />
</ItemGroup>
```

## 사용법

### 클라이언트

```csharp
using var transport = new NamedPipeTransport("my-service");
var conn = new IpcConnection(transport);

IpcResponse response = conn.Request("{\"action\":\"ping\"}");

Console.WriteLine(response.IsSuccess);  // true
Console.WriteLine(response.RawJson);    // {"success":true,"message":"pong"}
```

### 서버

```csharp
// 동기
var server = new NamedPipeServer("my-service");

server.Start(conn =>
{
   string request = conn.Receive();

   conn.Send(IpcResponse.Success("pong"));
});

// 비동기 — Task 완료까지 transport 유지
server.Start(async conn =>
{
   string request = conn.Receive();
   string result  = await ProcessAsync(request);

   conn.Send(result);
});
```

### IpcConnection (양쪽 공용)

```csharp
conn.Send(json);                    // 원시 JSON 전송
conn.Send(IpcResponse.Success());   // IpcResponse 전송
conn.Receive();                     // 수신
conn.Request(json);                 // 전송 + 수신 + 파싱
conn.RequestWithRetry(json);        // 실패 시 재시도
```

## Transport

| 타입 | Transport | Server | 용도 |
|------|-----------|--------|------|
| TCP | `TcpTransport` | `TcpServer` | 네트워크 / 원격 |
| Named Pipe | `NamedPipeTransport` | `NamedPipeServer` | 로컬 데몬 |
| UDS | `UdsTransport` | `UdsServer` | 로컬 (Linux/macOS) |
| Test | `TestTransport` | `TestServer` | 단위 테스트 |

모든 Transport는 `ITransport` (raw `Write`/`Read`), 모든 서버는 `IServer` (`Start`/`Stop`) 구현.

### Named Pipe 탐색

```csharp
NamedPipeTransport.Find("myapp-");       // 첫 번째 일치하는 파이프
NamedPipeTransport.FindAll("myapp-");    // 전체 목록
```

## 프레임 프로토콜

길이 접두사 프레이밍: `[4바이트 BE uint32 길이][UTF-8 본문]`

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
IpcResponse.Parse(json)                           // 응답 JSON 파싱
```

## JsonHelper

```csharp
// 직렬화
JsonHelper.Serialize(dict)                // Dictionary → JSON 문자열

// 파싱
JsonHelper.ParseSuccess(json)             // "success" 불리언 추출

// 읽기 (.NET 8+에서는 JsonElement / Standard 2.1에서는 JToken)
JsonHelper.GetInt(value, fallback)
JsonHelper.GetLong(value, fallback)
JsonHelper.GetFloat(value, fallback)
JsonHelper.GetDouble(value, fallback)
JsonHelper.GetString(value, fallback)
JsonHelper.GetBool(value, fallback)

// 쓰기
JsonHelper.Write(json, pretty);
JsonHelper.WriteError(json, pretty);
JsonHelper.Prettify(json);
```

## 호환성

| 타겟 | 버전 |
|------|------|
| .NET | 8.0+ |
| .NET Standard | 2.1 (Unity 2021+) |

.NET 8+에서는 System.Text.Json, .NET Standard 2.1에서는 Newtonsoft.Json 사용.

## 라이선스

[MIT](LICENSE)
