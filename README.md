# Network Compression Project

Simple C# client/server project for Network Programming.

The server accepts many clients at the same time. Each client sends a file size first, then sends the file bytes. The server compresses the received data using GZip, sends the compressed size back, then sends the compressed file bytes.

## Projects

- `CompressionServer`: console TCP server.
- `CompressionClient`: Windows Forms client with a small UI.

## Protocol

1. Client sends original file size as `Int64` - 8 bytes.
2. Client sends the file bytes.
3. Server compresses the file using `GZipStream`.
4. Server sends compressed file size as `Int64` - 8 bytes.
5. Server sends the compressed file bytes.

## How to Run

Build the solution:

```bash
dotnet build
```

This project was tested with the .NET 10 SDK.

Start the server:

```bash
dotnet run --project src/CompressionServer
```

Start the client:

```bash
dotnet run --project src/CompressionClient
```

Default server settings:

- Host: `127.0.0.1`
- Port: `9000`

Run the server first, then open the client, choose a file, and save the compressed result.
