using System;

namespace nora.lara.net {

    public class Subchannel {

        public static Subchannel Create(uint index) {
            return new Subchannel(index);
        }

        public SubchannelState State { get; private set; }
        public bool Empty { get { return State == SubchannelState.Empty; } }
        public bool Queued { get { return State == SubchannelState.Queued; } }
        public bool Blocked { get { return State == SubchannelState.Blocked; } }

        public uint Index { get; private set; }

        private byte[] data;
        private int offset;
        private int count;
        public uint SentIn { get; private set; }

        private Subchannel(uint index) {
            this.State = SubchannelState.Empty;
            this.Index = index;
        }

        public void Clear() {
            Preconditions.CheckArgument(Blocked);
            this.State = SubchannelState.Empty;
        }

        public void Queue(byte[] data, int offset, int count) {
            this.data = data;
            this.offset = offset;
            this.count = count;

            this.State = SubchannelState.Queued;
        }

        public void Requeue() {
            Preconditions.CheckArgument(Blocked);
            State = SubchannelState.Queued;
        }

        public void Write(Connection.Packet packet) {
            if (State == SubchannelState.Queued) {
                packet.Stream.WriteBool(true);
                WriteQueued(packet);
            } else {
                packet.Stream.WriteBool(false);
            }
        }

        private void WriteQueued(Connection.Packet packet) {
            Preconditions.CheckArgument(State == SubchannelState.Queued);

            if (offset != 0 || count != data.Length) {
                packet.Stream.WriteBool(true);
                WriteQueuedChunk(packet);
            } else {
                packet.Stream.WriteBool(false);
                WriteQueuedSingle(packet);
            }

            SentIn = packet.Seq;
            State = SubchannelState.Blocked;
        }

        private void WriteQueuedChunk(Connection.Packet packet) {
            int chunkOffset = (offset + Connection.BYTES_PER_CHUNK - 1) / Connection.BYTES_PER_CHUNK;
            int chunkCount = (count + Connection.BYTES_PER_CHUNK - 1) / Connection.BYTES_PER_CHUNK;

            packet.Stream.WriteBits((uint) chunkOffset, 18);
            packet.Stream.WriteBits((uint) chunkCount, 3);

            if (offset == 0) {
                packet.Stream.WriteBool(false);
                packet.Stream.WriteBool(false);

                packet.Stream.WriteBits((uint) data.Length, 26);
            }

            packet.Stream.Write(data, offset, count);
        }

        private void WriteQueuedSingle(Connection.Packet packet) {
            packet.Stream.WriteBool(false);

            packet.Stream.WriteBits((uint) count, 18);
            packet.Stream.Write(data, offset, count);
        }

        public enum SubchannelState {

            Empty,
            Queued,
            Blocked,
        }
    }
}
