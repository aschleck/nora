using log4net;
using nora.protos;
using ProtoBuf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using Snappy.Sharp;

namespace nora.lara.net {

    public class Connection : IDisposable {

        public static Connection CreateWith(ConnectionDetails details) {
            Connection connection = new Connection();

            for (uint i = 0; i < NUM_STREAMS; ++i) {
                connection.streams[i] = Stream.Create();
            }

            for (uint i = 0; i < NUM_SUBCHANNELS; ++i) {
                connection.subchannels[i] = Subchannel.Create(i);
            }

            connection.socket.Connect(details.Endpoint);

            connection.state = State.Opened;

            return connection;
        }

        public static Message ConvertProtoToMessage<T>(uint type, T proto) where T : IExtensible {
            byte[] bytes;
            using (var stream = Bitstream.Create()) {
                Serializer.SerializeWithLengthPrefix<T>(stream, proto, PrefixStyle.Base128);
                bytes = stream.ToBytes();
            }

            return new Message() {
                Type = type,
                Data = bytes,
            };
        }

        private static readonly ILog log = LogManager.GetLogger(typeof(Connection));

        private const int NUM_STREAMS = 2;
        private const int NUM_SUBCHANNELS = 8;

        private const int MAX_PACKET_SIZE = 1040;
        private const int PACKET_CHECKSUM_OFFSET = 4 + 4 + 1;
        private const int PACKET_RELIABLE_STATE_OFFSET = 4 + 4 + 1 + 2;
        private const int PACKET_HEADER_SIZE = 4 + 4 + 1 + 2 + 1; // seq ack flags checksum rs

        private const int CHUNKS_PER_MESSAGE = 4;
        public const int BYTES_PER_CHUNK = 1 << 8;
        private const int BYTES_PER_MESSAGE = CHUNKS_PER_MESSAGE * BYTES_PER_CHUNK;

        private const uint OOB_PACKET = 0xFFFFFFFF;
        private const uint SPLIT_PACKET = 0xFFFFFFFE;
        private const uint COMPRESSED_PACKET = 0xFFFFFFFD;
        private const uint LZSS_COMPRESSION = 0x53535A4C; // LZSS => SSZL

        private enum PacketFlags {

            IsReliable = 1,
        }

        private const uint ACK_EVERY = 6;

        public bool ShouldSendAcks { get; set; }

        private State state;
        private Socket socket;

        private uint sequenceOut; // sent
        private byte reliableStateOut;
        private uint lastAckRecv;

        private uint sequenceIn; // seen
        private uint receivedTotal;
        private byte reliableStateIn;
        private uint lastAckSent;

        private Stream[] streams;
        private Subchannel[] subchannels;
        private Dictionary<UInt32, SplitPacket> splitPackets;

        private object messageLock;
        private ConcurrentQueue<byte[]> messagesOutOfBand;
        private Queue<byte[]> messagesReliable;
        private Queue<byte[]> messagesUnreliable;

        private ConcurrentQueue<Message> receivedInBand;
        private ConcurrentQueue<byte[]> receivedOutOfBand;
        
        private SnappyDecompressor snappyDecompressor;

        private Connection() {
            this.ShouldSendAcks = false;

            this.state = State.Closed;
			this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            this.streams = new Stream[NUM_STREAMS];
            this.subchannels = new Subchannel[NUM_SUBCHANNELS];
            this.splitPackets = new Dictionary<uint, SplitPacket>();

            this.sequenceOut = 0;
            this.reliableStateOut = 0;
            this.lastAckRecv = 0;

            this.sequenceIn = 0;
            this.receivedTotal = 0;
            this.reliableStateIn = 0;
            this.lastAckSent = 0xFFFFFFFF;

            this.messageLock = new object();
            this.messagesOutOfBand = new ConcurrentQueue<byte[]>();
            this.messagesReliable = new Queue<byte[]>();
            this.messagesUnreliable = new Queue<byte[]>();

            this.receivedInBand = new ConcurrentQueue<Message>();
            this.receivedOutOfBand = new ConcurrentQueue<byte[]>();
            this.snappyDecompressor = new SnappyDecompressor();
        }

        public void Open() {
            Preconditions.CheckArgument(state == State.Opened);

            state = State.Handshaking;
            new Thread(Run).Start();
        }

        public void OpenChannel() {
            ShouldSendAcks = true;
        }

        public void Dispose() {
            if (state == State.Connected) {
                // TODO: Disconnect();
            }

            socket.Dispose();
        }

        public void SendReliably(params Message[] messages) {
            byte[] bytes;

            using (var stream = Bitstream.Create()) {
                foreach (var message in messages) {
                    stream.WriteVarUInt(message.Type);
                    stream.Write(message.Data);
                }

                bytes = stream.ToBytes();
            }

            lock (messageLock) {
//                log.Debug("Queuing reliably " + bytes.Length + " bytes");
                messagesReliable.Enqueue(bytes);
            }
        }

        public void SendUnreliably(params Message[] messages) {
            byte[] bytes;

            using (var stream = Bitstream.Create()) {
                foreach (var message in messages) {
                    stream.WriteVarUInt(message.Type);
                    stream.Write(message.Data);
                }

                bytes = stream.ToBytes();
            }

            lock (messageLock) {
//                log.Debug("Queuing unreliably " + bytes.Length + " bytes");
                messagesUnreliable.Enqueue(bytes);
            }
        }
        public void EnqueueOutOfBand(byte[] message) {
            messagesOutOfBand.Enqueue(message);
        }

        public List<Message> GetInBand() {
            List<Message> messages = new List<Message>();

            Message message;
            while (receivedInBand.TryDequeue(out message)) {
                messages.Add(message);
            }

            return messages;
        }

        public List<byte[]> GetOutOfBand() {
            List<byte[]> messages = new List<byte[]>();

            byte[] message;
            while (receivedOutOfBand.TryDequeue(out message)) {
                messages.Add(message);
            }

            return messages;
        }

        public byte[] WaitForOutOfBand() {
            byte[] b;

            while (!receivedOutOfBand.TryDequeue(out b)) {
                Thread.Sleep(5);
            }

            return b;
        }

        private void Run() {
            byte[] bytes = new byte[2048];

            while (state != State.Closed) {
                if (socket.Poll(1000, SelectMode.SelectRead)) {
                    int got = socket.Receive(bytes);
                    ReceivePacket(bytes, got);
                }

                SendQueued();

                if (ShouldSendAcks && lastAckSent + ACK_EVERY < receivedTotal) {
                    SendAck();
                }
            }
        }

        private void ReceivePacket(byte[] bytes, int length) {
            ++receivedTotal;

            using (var stream = Bitstream.CreateWith(bytes, length)) {
                uint type = stream.ReadUInt32();

                if (type == COMPRESSED_PACKET) {
                    uint method = stream.ReadUInt32();
                    Preconditions.CheckArgument(method == LZSS_COMPRESSION);

                    byte[] compressed = new byte[length - 8];
                    stream.Read(compressed, 0, compressed.Length);
                    Preconditions.CheckArgument(stream.Eof);

                    byte[] decompressed = Lzss.Decompress(compressed);
                    ProcessPacket(decompressed, decompressed.Length);
                } else if (type == SPLIT_PACKET) {
                    uint request = stream.ReadUInt32();
                    byte total = stream.ReadByte();
                    byte index = stream.ReadByte();
                    UInt16 size = stream.ReadUInt16();

                    SplitPacket split;
                    if (!splitPackets.ContainsKey(request)) {
                        split = new SplitPacket() {
                            Request = request,
                            Total = total,
                            Received = 0,
                            Data = new byte[total][],
                            Present = new bool[total]
                        };
                        splitPackets[request] = split;
                    } else {
                        split = splitPackets[request];
                    }

                    byte[] buffer = new byte[Math.Min(size, (stream.Remain + 7) / 8)];
                    stream.Read(buffer, 0, buffer.Length);
                    split.Data[index] = buffer;

                    if (!split.Present[index]) {
                        ++split.Received;
                        split.Present[index] = true;
                    }

                    if (split.Received == split.Total) {
                        byte[] full = split.Data.SelectMany(b => b).ToArray();
                        ReceivePacket(full, full.Length);
                        splitPackets.Remove(request);
                    }
                } else if (type == OOB_PACKET) {
                    byte[] data = new byte[stream.Length - 4];
                    stream.Read(data, 0, data.Length);

                    receivedOutOfBand.Enqueue(data);
                } else {
                    ProcessPacket(bytes, length);
                }
            }
        }

        private void ProcessPacket(byte[] bytes, int length) {
            using (var stream = Bitstream.CreateWith(bytes, length)) {
                uint seq = stream.ReadUInt32();
                uint ack = stream.ReadUInt32();

                byte flags = stream.ReadByte();
                ushort checksum = stream.ReadUInt16();

                long at = stream.Position;
                ushort computed = CrcUtils.Compute16(stream);
                stream.Position = at;

                if (checksum != computed) {
                    log.WarnFormat(
                        "failed checksum:"
                            + "recv seq {0} ack {1} flags {2:x} checksum {3:x} computed {4:x}", 
                        seq, ack, flags, checksum, computed);
                    return;
                }

                byte reliableState = stream.ReadByte();

                if ((flags & 0x10) == 0x10) {
                    log.WarnFormat(
                        "choke {0}: recv seq {1} ack {2} flags {3:x}",
                        stream.ReadByte(), seq, ack, flags);
                }

                if (seq < sequenceIn) {
                    // We no longer care.
                    log.WarnFormat("dropped: recv seq {0} ack {1}", seq, ack);
                    return;
                }

                for (byte i = 0; i < subchannels.Length; ++i) {
                    Subchannel channel = subchannels[i];
                    int mask = 1 << i;

                    if ((reliableStateOut & mask) == (reliableState & mask)) {
                        if (channel.Blocked) {
                            Preconditions.CheckArgument(ack >= channel.SentIn);

                            channel.Clear();
                        }
                    } else {
                        if (channel.Blocked && channel.SentIn < ack) {
                            reliableStateOut = Flip(reliableStateOut, i);
                            channel.Requeue();
                        }
                    }
                }

                if ((flags & (uint)PacketFlags.IsReliable) != 0) {
                    uint bit = stream.ReadBits(3);
                    //Debug.WriteLine("  reliable, flip {0}. {1} => {2}", bit, reliableStateIn, Flip(reliableStateIn, bit));
                    reliableStateIn = Flip(reliableStateIn, bit);
                    
                    for (int i = 0; i < streams.Length; ++i) {
                        Nullable<Stream.Message> message = streams[i].Receive(stream);

                        if (message.HasValue) {
                            ProcessMessage(message.Value);
                        }
                    }
                }

                while (stream.HasByte()) {
                    HandleMessage(stream);
                }

                if (!stream.Eof) {
                    byte remain = (byte)stream.Remain;
                    int expect = (1 << remain) - 1;
                    Preconditions.CheckArgument(stream.ReadBits(remain) == expect);
                }

                lastAckRecv = ack;
                sequenceIn = seq;
            }
        }

        private void ProcessMessage(Stream.Message message) {
            byte[] data;
            if (!message.IsCompressed) {
                data = message.Data;
            } else {
                data = snappyDecompressor.Decompress(message.Data, 0, message.Data.Length);

                Preconditions.CheckArgument(message.DecompressedLength == data.Length);
            }

            using (var stream = Bitstream.CreateWith(data)) {
                while (stream.HasByte()) {
                    HandleMessage(stream);
                }

                if (!stream.Eof) {
                    byte remain = (byte)stream.Remain;
                    int expect = (1 << remain) - 1;
                    Preconditions.CheckArgument(stream.ReadBits(remain) == expect);
                }
            }
        }

        private void HandleMessage(Bitstream stream) {
            uint type = stream.ReadVarUInt();
            uint length = stream.ReadVarUInt();

            byte[] bytes = new byte[length];
            stream.Read(bytes, 0, (int) length);

            receivedInBand.Enqueue(new Message {
                Type = type,
                Data = bytes,
            });
        }

        private void SendAck() {
            var packet = MakePacket();

            packet.Stream.WriteByte(0);
            Serializer.SerializeWithLengthPrefix<CNETMsg_NOP>(packet.Stream, new CNETMsg_NOP(), PrefixStyle.Base128);
            packet.Stream.WriteByte(0);
            Serializer.SerializeWithLengthPrefix<CNETMsg_NOP>(packet.Stream, new CNETMsg_NOP(), PrefixStyle.Base128);

            SendDatagram(packet);
        }

        private void SendQueued() {
            SendMessagesOutOfBand();
            SendMessagesInBand();
        }

        private void SendMessagesOutOfBand() {
            byte[] message;

            while (messagesOutOfBand.TryDequeue(out message)) {
                using (var stream = Bitstream.Create()) {
                    stream.WriteUInt32(OOB_PACKET);
                    stream.Write(message);

                    byte[] bytes = new byte[stream.Length];
                    stream.Position = 0;
                    stream.Read(bytes, 0, bytes.Length);
                    socket.Send(bytes);
                }
            }
        }

        private void SendMessagesInBand() {
            Subchannel toSend = null;

            // see if there are any subchannels where we could send messages
            if (subchannels.Any(channel => !channel.Blocked)) {
                Subchannel queued = subchannels.FirstOrDefault(channel => channel.Queued);

                if (queued != null) {
                    toSend = queued;
                } else if (messagesReliable.Count > 0) {
                    toSend = subchannels.First(channel => channel.Empty);

                    byte[] bytes;
                    lock (messageLock) {
                        bytes = TakeMessages(messagesReliable,
                            BYTES_PER_MESSAGE);
                    }

                    Preconditions.CheckArgument(bytes.Length > 0); // because singles only
                    toSend.Queue(bytes, 0, bytes.Length);
                }
            }

            if (toSend == null && messagesUnreliable.Count == 0) {
                return;
            }

            var packet = MakePacket();

            if (toSend != null) {
                packet.Flags |= (int)PacketFlags.IsReliable;

                packet.Stream.WriteBits(toSend.Index, 3);
                reliableStateOut = Flip(reliableStateOut, toSend.Index);

                foreach (var channel in streams) {
                    toSend.Write(packet);
                }
            }

            if (messagesUnreliable.Count > 0) {
                long space = MAX_PACKET_SIZE - (packet.Stream.Position + 7) / 8;

                byte[] bytes;
                lock (messageLock) {
                    bytes = TakeMessages(messagesUnreliable, space);
                }

                packet.Stream.Write(bytes);
            }

            SendDatagram(packet);
        }

        private void SendDatagram(Packet packet) {
            lastAckSent = receivedTotal;

            packet.Stream.Position = PACKET_RELIABLE_STATE_OFFSET * 8;
            packet.Stream.WriteByte(reliableStateIn);

            packet.Stream.Position = PACKET_RELIABLE_STATE_OFFSET * 8;
            ushort crc = CrcUtils.Compute16(packet.Stream);

            packet.Stream.Position = 0;
            packet.Stream.WriteUInt32(packet.Seq);
            packet.Stream.WriteUInt32(packet.Ack);
            packet.Stream.WriteByte(packet.Flags);
            packet.Stream.WriteUInt16(crc);

            byte[] bytes = new byte[packet.Stream.Length];
            packet.Stream.Position = 0;
            packet.Stream.Read(bytes, 0, bytes.Length);

            socket.Send(bytes);
        }

        private void SendRawDatagram(Bitstream stream) {
            byte[] bytes = new byte[stream.Length];

            stream.Position = 0;
            stream.Read(bytes, 0, (int)stream.Length);

            socket.Send(bytes);
        }

        private Packet MakePacket() {
            var packet = new Packet {
                Seq = ++sequenceOut,
                Ack = sequenceIn,

                Stream = Bitstream.Create(),
            };

            packet.Stream.SetLength(PACKET_HEADER_SIZE);
            packet.Stream.Position = PACKET_HEADER_SIZE * 8;

            return packet;
        }

        public struct Packet {

            public uint Seq { get; set; }
            public uint Ack { get; set; }
            public byte Flags { get; set; }

            public Bitstream Stream { get; set; }
        }

        public struct Message {

            public uint Type { get; set; }
            public byte[] Data { get; set; }
        }

        private class SplitPacket {

            public uint Request { get; set; }
            public byte Received { get; set; }
            public byte Total { get; set; }
            public byte[][] Data { get; set; }
            public bool[] Present { get; set; }
        }

        private enum State {

            Closed,
            Opened,
            Handshaking,
            Connected,
        }

        private static byte[] TakeMessages(Queue<byte[]> messages, long maxLength) {
            Preconditions.CheckArgument(messages.Count > 0);

            using (var stream = new MemoryStream()) {
                while (messages.Count > 0) {
                    byte[] peek = messages.Peek();

                    if (stream.Position + peek.Length > maxLength) {
                        break;
                    }

                    byte[] head = messages.Dequeue();
                    Preconditions.CheckArgument(peek == head);

                    //log.Debug("Taking a message with " + head.Length + " bytes");
                    stream.Write(head, 0, head.Length);
                }

                return stream.ToArray();
            }
        }

        private static byte Flip(byte b, uint index) {
            Preconditions.CheckArgument(index < 32);
            return (byte)(b ^ (1 << (int)index));
        }
    }
}
