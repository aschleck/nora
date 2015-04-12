using System.Collections.Immutable;
using System.Net;

namespace nora.lara {

    public class ConnectionDetails {

        public EndPoint Endpoint { get; set; }
        public ulong SteamId { get; set; }
        public string Nickname { get; set; }
        public ulong SteamworksSessionId { get; set; }
        public ImmutableArray<byte> Ticket { get; set; }
        public string Secret { get; set; }
    }
}
