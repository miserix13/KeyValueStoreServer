using System.Net;
using System.Net.Sockets;
using System.Text;
using Tenray.ZoneTree;
using Tenray.ZoneTree.Comparers;
using Tenray.ZoneTree.Serializers;

namespace KeyValueStoreServer
{
    public class KeyValueServer
    {
        private readonly TcpListener listener;
        private readonly ITransactionalZoneTree<Guid, Memory<byte>> zoneTree;
        private readonly CancellationTokenSource cts = new();
        private readonly object startStopLock = new();
        private Task? acceptLoopTask;
        private bool isStarted;

        public KeyValueServer(ServerOptions options) :
            base()
        {
            this.listener = new TcpListener(new IPEndPoint(IPAddress.Any, options.Port));
            this.zoneTree = new ZoneTreeFactory<Guid, Memory<byte>>().SetDataDirectory(options.DataDirectory).SetComparer(new GuidComparerAscending()).SetKeySerializer(new StructSerializer<Guid>()).SetValueSerializer(new ByteArraySerializer()).OpenOrCreateTransactional();
        }

        public async Task StartAsync()
        {
            lock (this.startStopLock)
            {
                if (this.isStarted)
                    return;

                this.listener.Start();
                this.acceptLoopTask = this.AcceptLoopAsync(this.cts.Token);
                this.isStarted = true;
            }

            await Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            Task? acceptTask = null;

            lock (this.startStopLock)
            {
                if (!this.isStarted)
                    return;

                this.cts.Cancel();
                this.listener.Stop();
                acceptTask = this.acceptLoopTask;
                this.isStarted = false;
            }

            if (acceptTask is not null)
            {
                try
                {
                    await acceptTask;
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        private async Task AcceptLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await this.listener.AcceptTcpClientAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (SocketException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                _ = Task.Run(() => this.HandleClientAsync(client, cancellationToken), cancellationToken);
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            using (client)
            await using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true))
            using (var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true) { AutoFlush = true })
            {
                await writer.WriteLineAsync("OK KeyValueServer Ready");

                while (!cancellationToken.IsCancellationRequested)
                {
                    string? line;
                    try
                    {
                        line = await reader.ReadLineAsync().WaitAsync(cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (IOException)
                    {
                        break;
                    }

                    if (line is null)
                        break;

                    var shouldClose = await this.ProcessCommandAsync(line, writer);
                    if (shouldClose)
                        break;
                }
            }
        }

        private async Task<bool> ProcessCommandAsync(string line, StreamWriter writer)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                await writer.WriteLineAsync("ERROR Empty command");
                return false;
            }

            var parts = line.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            var command = parts[0].ToUpperInvariant();

            switch (command)
            {
                case "PING":
                    await writer.WriteLineAsync("PONG");
                    return false;

                case "QUIT":
                    await writer.WriteLineAsync("BYE");
                    return true;

                case "GET":
                    if (parts.Length < 2)
                    {
                        await writer.WriteLineAsync("ERROR Usage: GET <guid>");
                        return false;
                    }

                    if (!Guid.TryParse(parts[1], out var getKey))
                    {
                        await writer.WriteLineAsync("ERROR Invalid guid");
                        return false;
                    }

                    var transactionId = this.zoneTree.BeginTransaction();
                    try
                    {
                        if (this.zoneTree.TryGet(transactionId, in getKey, out var value))
                        {
                            await writer.WriteLineAsync($"VALUE {Convert.ToBase64String(value.ToArray())}");
                        }
                        else
                        {
                            await writer.WriteLineAsync("NOT_FOUND");
                        }
                    }
                    finally
                    {
                        this.zoneTree.Rollback(transactionId);
                    }

                    return false;

                case "SET":
                    if (parts.Length < 3)
                    {
                        await writer.WriteLineAsync("ERROR Usage: SET <guid> <base64>");
                        return false;
                    }

                    if (!Guid.TryParse(parts[1], out var setKey))
                    {
                        await writer.WriteLineAsync("ERROR Invalid guid");
                        return false;
                    }

                    byte[] setBytes;
                    try
                    {
                        setBytes = Convert.FromBase64String(parts[2]);
                    }
                    catch (FormatException)
                    {
                        await writer.WriteLineAsync("ERROR Value must be base64");
                        return false;
                    }

                    var setValue = setBytes.AsMemory();
                    this.zoneTree.UpsertAutoCommit(in setKey, in setValue);
                    await writer.WriteLineAsync("OK");
                    return false;

                case "DELETE":
                    if (parts.Length < 2)
                    {
                        await writer.WriteLineAsync("ERROR Usage: DELETE <guid>");
                        return false;
                    }

                    if (!Guid.TryParse(parts[1], out var deleteKey))
                    {
                        await writer.WriteLineAsync("ERROR Invalid guid");
                        return false;
                    }

                    this.zoneTree.DeleteAutoCommit(in deleteKey);
                    await writer.WriteLineAsync("OK");
                    return false;

                default:
                    await writer.WriteLineAsync("ERROR Unknown command");
                    return false;
            }
        }
    }
}
