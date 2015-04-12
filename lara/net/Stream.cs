using log4net;
using System;
using System.Text;

namespace nora.lara.net {

    public class Stream {

        public static Stream Create() {
            return new Stream();
        }

        private static readonly ILog log = LogManager.GetLogger(typeof(Stream));
        
        public bool Receiving { get; private set; }
        private ChunkedHeader header;
        private byte[] dataIn;
        private bool[] dataReceived;
        private int countReceived;

        private Stream() {
        }

        public Nullable<Message> Receive(Bitstream stream) {
            bool hasData = stream.ReadBool();

            if (!hasData) {
                return null;
            }

            if (stream.ReadBool()) {
                return ReadChunk(stream);
            } else {
                return ReadSingle(stream);
            }
        }

        private void ReadChunkHeader(Bitstream stream) {
            header = new ChunkedHeader();

            header.IsFile = stream.ReadBool();
            if (header.IsFile) {
                uint filenameLength = stream.ReadUInt32();
                byte[] filename = new byte[filenameLength + 1]; // semantically wrong. should be
                                                                // 0x104
                stream.Read(filename, 0, (int)filenameLength); // and then read to end of string
                filename[filenameLength] = 0; // whatever 
                header.Filename = Encoding.UTF8.GetString(filename);
                log.ErrorFormat("read filename: \"{0}\" ({1})", filenameLength, header.Filename);
                throw new NotImplementedException();
            }

            header.IsCompressed = stream.ReadBool();
            if (header.IsCompressed) {
                header.DecompressedLength = stream.ReadBits(26);
                log.DebugFormat(
                    "chunkheader compressed: decompressed len {0}", 
                    header.DecompressedLength);
            }

            header.ByteLength = stream.ReadBits(26);
            header.ChunkCount = 
                (header.ByteLength + Connection.BYTES_PER_CHUNK - 1) /
                Connection.BYTES_PER_CHUNK;
            log.DebugFormat(
                "chunkheader len {0} expecting {1} chunks", 
                header.ByteLength, header.ChunkCount);

            Receiving = true;
            dataIn = new byte[header.ByteLength];
            dataReceived = new bool[header.ChunkCount];
            countReceived = 0;
        }

        private Nullable<Message> ReadChunk(Bitstream stream) {
            uint offset = stream.ReadBits(18);
            uint count = stream.ReadBits(3);

            log.DebugFormat("chunk start = {0} end = {1}", offset, count);

            if (offset == 0) {
                Preconditions.CheckArgument(!Receiving);
                ReadChunkHeader(stream);
            } else {
                Preconditions.CheckArgument(Receiving);
            }

            uint byteOffset = offset * Connection.BYTES_PER_CHUNK;

            uint byteCount;
            if (offset + count < header.ChunkCount) {
                byteCount = count * Connection.BYTES_PER_CHUNK;
            } else {
                byteCount = header.ByteLength - byteOffset;
            }

            stream.Read(dataIn, (int)byteOffset, (int)byteCount);

            for (uint i = offset;
                    i < offset + count;
                    ++i) {
                if (!dataReceived[i]) {
                    dataReceived[i] = true;
                    ++countReceived;
                }
            }

            if (countReceived == header.ChunkCount) {
                log.Debug("chunk complete");
                Receiving = false;
                return new Message {
                    IsCompressed = header.IsCompressed,
                    DecompressedLength = header.DecompressedLength,

                    Data = dataIn,
                };
            } else {
                log.DebugFormat("chunk has {0}/{1}", countReceived, header.ChunkCount);
                return null;
            }
        }

        private Message ReadSingle(Bitstream stream) {
            bool isCompressed = stream.ReadBool();
            
            if (isCompressed) {
                uint uncompressed_length = stream.ReadBits(26);
                uint length = stream.ReadBits(18);

                byte[] data = new byte[length];
                stream.Read(data, 0, (int) length);

                return new Message {
                    IsCompressed = false,
                    Data = Snappy.Sharp.Snappy.Uncompress(data)
                };
            } else {
                uint length = stream.ReadBits(18);
                Preconditions.CheckArgument(length < Int32.MaxValue);

                byte[] data = new byte[length];
                stream.Read(data, 0, (int)length);

                return new Message {
                    IsCompressed = false,
                    Data = data,
                };
            }
        }

        private struct ChunkedHeader {

            public uint ChunkCount { get; set; }
            public uint ByteLength { get; set; }

            public bool IsCompressed { get; set; }
            public uint DecompressedLength { get; set; }

            public bool IsFile { get; set; }
            public string Filename { get; set; }
        }

        public struct Message {

            public bool IsCompressed { get; set; }
            public uint DecompressedLength { get; set; }

            public byte[] Data { get; set; }
        }
    }
}
