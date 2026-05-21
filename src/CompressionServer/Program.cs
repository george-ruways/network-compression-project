using System.IO.Compression;
using System.Net;
using System.Net.Sockets;

namespace CompressionServer;

internal static class Program
{
    private const int DefaultPort = 9000;
    private const int BufferSize = 81920;

    private static void Main(string[] args)
    {
        int port = ReadPort(args);
        TcpListener listener = new TcpListener(IPAddress.Any, port);

        listener.Start();
        Console.WriteLine("Multi-threaded Compression Server");
        Console.WriteLine($"Listening on port {port}");
        Console.WriteLine("Press Ctrl+C to stop the server.");

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();

            Thread worker = new Thread(() => HandleClient(client));
            worker.IsBackground = true;
            worker.Start();
        }
    }

    private static int ReadPort(string[] args)
    {
        if (args.Length > 0 && int.TryParse(args[0], out int port))
        {
            return port;
        }

        return DefaultPort;
    }

    private static void HandleClient(TcpClient client)
    {
        string clientName = client.Client.RemoteEndPoint?.ToString() ?? "unknown client";
        string tempFile = Path.GetTempFileName();

        try
        {
            using (client)
            using (NetworkStream stream = client.GetStream())
            {
                Console.WriteLine($"[{DateTime.Now:T}] Connected: {clientName}");

                long originalSize = ReadInt64(stream);
                if (originalSize < 0)
                {
                    throw new InvalidDataException("Invalid file size received.");
                }

                ReceiveAndCompress(stream, tempFile, originalSize);

                long compressedSize = new FileInfo(tempFile).Length;
                WriteInt64(stream, compressedSize);
                SendFile(stream, tempFile);

                Console.WriteLine(
                    $"[{DateTime.Now:T}] Done: {clientName}, {originalSize} bytes to {compressedSize} bytes");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now:T}] Error with {clientName}: {ex.Message}");
        }
        finally
        {
            TryDelete(tempFile);
        }
    }

    private static void ReceiveAndCompress(NetworkStream stream, string outputPath, long bytesToRead)
    {
        byte[] buffer = new byte[BufferSize];

        using FileStream fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        using GZipStream gzipStream = new GZipStream(fileStream, CompressionLevel.Optimal);

        long remaining = bytesToRead;
        while (remaining > 0)
        {
            int wanted = remaining > buffer.Length ? buffer.Length : (int)remaining;
            int read = stream.Read(buffer, 0, wanted);

            if (read == 0)
            {
                throw new EndOfStreamException("Client disconnected before sending the whole file.");
            }

            gzipStream.Write(buffer, 0, read);
            remaining -= read;
        }
    }

    private static void SendFile(NetworkStream stream, string filePath)
    {
        byte[] buffer = new byte[BufferSize];

        using FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        int read;
        while ((read = fileStream.Read(buffer, 0, buffer.Length)) > 0)
        {
            stream.Write(buffer, 0, read);
        }
    }

    private static long ReadInt64(NetworkStream stream)
    {
        byte[] sizeBytes = ReadExact(stream, sizeof(long));
        return BitConverter.ToInt64(sizeBytes, 0);
    }

    private static void WriteInt64(NetworkStream stream, long value)
    {
        byte[] sizeBytes = BitConverter.GetBytes(value);
        stream.Write(sizeBytes, 0, sizeBytes.Length);
    }

    private static byte[] ReadExact(NetworkStream stream, int count)
    {
        byte[] buffer = new byte[count];
        int offset = 0;

        while (offset < count)
        {
            int read = stream.Read(buffer, offset, count - offset);
            if (read == 0)
            {
                throw new EndOfStreamException("Connection closed while reading data.");
            }

            offset += read;
        }

        return buffer;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Nothing important to do if Windows is still holding the temp file.
        }
    }
}

