using SteamKit2;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net;

namespace nora.clara {

    class AuthTicket {

        public static ImmutableArray<byte> CreateAuthTicket(ImmutableArray<byte> token, IPAddress ip) {
            uint sessionSize = 4 + // unknown 1
                               4 + // unknown 2
                               4 + // external IP
                               4 + // filler
                               4 + // timestamp
                               4; // connection count
            
            MemoryStream stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream)) {
                writer.Write(token.Length);
                writer.Write(token.ToArray());

                writer.Write(sessionSize);
                writer.Write(1);
                writer.Write(2);

                byte[] externalBytes = ip.GetAddressBytes();
                writer.Write(externalBytes.Reverse().ToArray());

                writer.Write((int) 0);
                writer.Write(2038 /* ms since connected to steam? */);
                writer.Write(1 /* connection count to steam? */);
            }

            return stream.ToArray().ToImmutableArray();
        }

        public static ImmutableArray<byte> CreateServerTicket(
                SteamID id, ImmutableArray<byte> auth, byte[] ownershipTicket) {
            long size = 8 + // steam ID
                        auth.Length +
                        4 + // length of ticket
                        ownershipTicket.Length;

            MemoryStream stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream)) {
                writer.Write((ushort) size);
                writer.Write(id.ConvertToUInt64());

                writer.Write(auth.ToArray());

                writer.Write(ownershipTicket.Length);
                writer.Write(ownershipTicket);

                writer.Write(0);
            }

            return stream.ToArray().ToImmutableArray();
        }
    }
}
