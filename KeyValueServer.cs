using System.Net;
using System.Net.Sockets;
using Tenray.ZoneTree;
using Tenray.ZoneTree.Comparers;
using Tenray.ZoneTree.Serializers;

namespace KeyValueStoreServer
{
    public class KeyValueServer
    {
        private readonly TcpListener listener;
        private readonly ITransactionalZoneTree<Guid, Memory<byte>> zoneTree;

        public KeyValueServer(ServerOptions options) :
            base()
        {
            this.listener = new TcpListener(new IPEndPoint(IPAddress.Any, options.Port));
            this.zoneTree = new ZoneTreeFactory<Guid, Memory<byte>>().SetDataDirectory(options.DataDirectory).SetComparer(new GuidComparerAscending()).SetKeySerializer(new StructSerializer<Guid>()).SetValueSerializer(new ByteArraySerializer()).OpenOrCreateTransactional();
        }

        public async Task StartAsync()
        {
            this.listener.Start();
        }

        public async Task StopAsync()
        {
            this.listener.Stop();
        }
    }
}
