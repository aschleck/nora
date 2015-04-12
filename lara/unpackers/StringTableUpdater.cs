using log4net;
using nora.lara.state;
using System;
using System.Collections.Generic;

namespace nora.lara.unpackers {

    public class StringTableUpdater {

        private const int KEY_HISTORY_SIZE = 32;
        private const int MAX_KEY_SIZE = 1024;
        private const int MAX_VALUE_SIZE = 16384;

        private static readonly ILog log = LogManager.GetLogger(typeof(StringTableUpdater));

        public void Update(StringTable table, int numEntries, byte[] data) {
            using (var stream = Bitstream.CreateWith(data)) {
                bool option = stream.ReadBool();
                if (option) {
                    throw new ArgumentException("Unknown option " + option);
                }

                List<string> keyHistory = new List<string>();

                uint entryId = UInt32.MaxValue;
                uint read = 0;

                while (read < numEntries) {
                    if (!stream.ReadBool()) {
                        entryId = stream.ReadBits(table.EntryBits);
                    } else {
                        entryId = unchecked(entryId + 1);
                    }

                    Preconditions.CheckArgument(entryId < table.MaxEntries);

                    string key = ReadKeyIfIncluded(stream, keyHistory);
                    byte[] value = ReadValueIfIncluded(stream, table.UserDataFixedSize,
                        table.UserDataSizeBits);

                    if (entryId < table.Count) {
                        StringTable.Entry entry = table.Get(entryId);

                        if (key != null) {
                            Preconditions.CheckArgument(key.Equals(entry.Key));
                        }

                        if (value != null) {
                            entry.Value = value;
                        }
                    } else {
                        table.Put(entryId, new StringTable.Entry(key, value));
                    }

                    ++read;
                }
            }
        }

        private string ReadKeyIfIncluded(Bitstream stream, List<string> keyHistory) {
            bool has_key = stream.ReadBool();

            if (!has_key) {
                return null;
            } 

            bool is_substring = stream.ReadBool();

            string key;

            if (!is_substring) {
                key = stream.ReadString();
            } else {
                int fromIndex = (int) stream.ReadBits(5);
                int fromLength = (int) stream.ReadBits(5);
                key = keyHistory[fromIndex].Substring(0, fromLength);

                key += stream.ReadString();
            }

            Preconditions.CheckArgument(key.Length <= MAX_KEY_SIZE);

            if (keyHistory.Count == KEY_HISTORY_SIZE) {
                keyHistory.RemoveAt(0);
            }

            keyHistory.Add(key);

            return key;
        }

        private byte[] ReadValueIfIncluded(Bitstream stream, bool userDataFixedSize,
                uint userDataSizeBits) {
            bool has_value = stream.ReadBool();

            if (!has_value) {
                return null;
            }

            uint length;
            uint bitLength;

            if (userDataFixedSize) {
                length = (userDataSizeBits + 7) / 8;
                bitLength = userDataSizeBits;
            } else {
                length = stream.ReadBits(14);
                bitLength = 8 * length;
            }

            Preconditions.CheckArgument(length <= MAX_VALUE_SIZE);

            return stream.ReadManyBits(bitLength);
        }
    }
}
