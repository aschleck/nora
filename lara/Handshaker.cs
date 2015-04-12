using log4net;
using nora.lara.machine;
using nora.lara.net;
using nora.lara.state;
using nora.protos;
using ProtoBuf;
using System;
using System.Text;

namespace nora.lara {

    public class Handshaker : Processor {

        private static readonly ILog log = LogManager.GetLogger(typeof(Handshaker));

        private const char C2S_REQUEST = 'q';
        private const char S2C_CHALLENGE = 'A';
        private const char C2S_CONNECT = 'k';
        private const char S2C_ACCEPT = 'B';
        private const char S2C_REJECT = '9';

        private const uint SOURCE_PROTOCOL = 0x5A4F4933;
        private const uint SOURCE_VERSION = 0x00000029;
        private const uint STEAM_VERSION = 0x00000003;

        private ConnectionDetails details;
        private Client client;
        private Connection connection;

        private uint client_challenge;
        private uint server_challenge;
        private ulong server_id;

        public Handshaker(
            ConnectionDetails details,
            Client client,
            Connection connection) {
            this.details = details;
            this.connection = connection;
            this.client = client;
            this.client_challenge = (uint) new Random().Next();
        }

        public void RequestHandshake() {
            connection.Open();

            using (var stream = Bitstream.Create()) {
                stream.WriteChar(C2S_REQUEST);

                stream.WriteUInt32(client_challenge);

                foreach (char c in "0000000000") {
                    stream.WriteChar(c);
                }
                stream.WriteByte(0);

                connection.EnqueueOutOfBand(stream.ToBytes());
            }
        }

        public void RespondHandshake() {
            using (var stream = Bitstream.Create()) {
                stream.WriteChar(C2S_CONNECT);
                stream.WriteUInt32(SOURCE_VERSION);
                stream.WriteUInt32(STEAM_VERSION);
                stream.WriteUInt32(server_challenge);
                stream.WriteUInt32(client_challenge);

                stream.Write(Encoding.UTF8.GetBytes(client.CVars["name"]));
                stream.WriteByte(0);

                stream.Write(Encoding.UTF8.GetBytes(details.Secret.ToString()));
                stream.WriteByte(0);

                // num players
                stream.WriteByte(1);

                // cvars
                stream.WriteByte((byte)CLC_Messages.clc_SplitPlayerConnect);

                var split = new CCLCMsg_SplitPlayerConnect();
                split.convars = client.ExposeCVars();
                Serializer.SerializeWithLengthPrefix(stream, split, PrefixStyle.Base128);
                
                connection.EnqueueOutOfBand(stream.ToBytes());
            }
        }

        public Event? Process(byte[] response) {
            using (var stream = Bitstream.CreateWith(response)) {
                byte type = stream.ReadByte();

                if (type == S2C_CHALLENGE) {
                    Preconditions.CheckArgument(stream.ReadUInt32() == SOURCE_PROTOCOL,
                        "Packet not SOURCE_PROTOCOL");
                    server_challenge = stream.ReadUInt32();
                    Preconditions.CheckArgument(stream.ReadUInt32() == client_challenge,
                        "Packet doesn't match client challenge");
                    Preconditions.CheckArgument(stream.ReadUInt32() == STEAM_VERSION,
                        "Packet STEAM_VERSION mismatch");
                    server_id = stream.ReadUInt64();
                    log.Info("challenge mystery byte is " + stream.ReadByte());
                    Preconditions.CheckArgument(stream.Eof, "Packet S2C_CHALLENGE continues");
                    return Event.HANDSHAKE_CHALLENGE;
                } else if (type == S2C_ACCEPT) {
                    Preconditions.CheckArgument(stream.ReadUInt32() == client_challenge,
                        "Packet doesn't match client challenge");
                    Preconditions.CheckArgument(stream.Eof, "Packet S2C_ACCEPT continues");
                    return Event.HANDSHAKE_COMPLETE;
                } else if (type == S2C_REJECT) {
                    Preconditions.CheckArgument(stream.ReadUInt32() == client_challenge,
                        "Packet doesn't match client challenge");

                    log.Error("Rejected:\n" + stream.ReadString());
                    return Event.REJECTED;
                } else {
                    throw new ArgumentException("Unknown response type " + type);
                }
            }
        }

        public Event? Process(Connection.Message message) {
            throw new NotImplementedException();
        }
    }
}
