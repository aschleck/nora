using log4net;
using nora.lara.state;
using System;
using System.Collections.Generic;

namespace nora.lara.unpackers {

    public class EntityUpdater {

        private static ILog log = LogManager.GetLogger(typeof(EntityUpdater));

        // TODO: Should this be from ServerInfo.max_classes?
        private byte ClassBitLength { get { return Util.Log2((uint) client.Classes.Count); } }

        private Client client;
        private PropertyValueUnpacker unpacker;

        public EntityUpdater(Client client) {
            this.client = client;
            this.unpacker = new PropertyValueUnpacker();
        }

        public void Update(Bitstream stream, uint baseline, bool updateBaseline, uint updated, bool isDelta) {
            uint id = UInt32.MaxValue;
            uint found = 0;

            while (found < updated) {
                var flags = ReadHeader(ref id, stream);

                if (flags.HasFlag(UpdateFlag.EnterPvs)) {
                    ReadEnterPvs(id, baseline, updateBaseline, stream);
                } else if (flags.HasFlag(UpdateFlag.LeavePvs)) {
                    if (flags.HasFlag(UpdateFlag.Delete)) {
                        Delete(id);
                    }
                } else {
                    ReadUpdate(id, stream);
                }

                ++found;
            }

            if (isDelta) {
                while (stream.ReadBool()) {
                    id = stream.ReadBits(11);
                    Delete(id);
                }
            }
        }

        private UpdateFlag ReadHeader(ref uint id, Bitstream stream) {
            uint value = stream.ReadBits(6);

            if ((value & 0x30) > 0) {
                uint a = (value >> 4) & 3;
                uint b = (uint) ((a == 3) ? 16 : 0);

                value = (stream.ReadBits((byte) (4 * a + b)) << 4) | (value & 0xF);
            }

            id = unchecked(id + value + 1);

            var flags = UpdateFlag.None;

            if (!stream.ReadBool()) {
                if (stream.ReadBool()) {
                    flags |= UpdateFlag.EnterPvs;
                }
            } else {
                flags |= UpdateFlag.LeavePvs;

                if (stream.ReadBool()) {
                    flags |= UpdateFlag.Delete;
                }
            }

            return flags;
        }

        private void Delete(uint id) {
            client.Deleted.Add(id);
            if (!client.Slots.ContainsKey(id)) {
                return;
            }

            int clazz = (int) client.Slots[id].Entity.Class.Id;
            foreach (var info in client.FlatTables[clazz].Properties) {
                client.Properties.Remove(new Client.PropertyHandle() {
                    Entity = id,
                    Table = info.Origin.NetTableName,
                    Name = info.VarName
                });
            }
            client.Slots[id].Live = false;
        }

        private void ReadEnterPvs(uint id, uint baseline, bool update_baseline, Bitstream stream) {
            var clazz = client.Classes[(int) stream.ReadBits(this.ClassBitLength)];
            var serial = stream.ReadBits(10);

            log.Debug(String.Format("{0} entering pvs as a {1} (serial {2})", id, clazz.ClassName, serial));

            Create(id, clazz, baseline);
            ReadAndUnpackFields(client.Slots[id].Entity, stream);

            if (update_baseline) {
                client.Slots[id].Baselines[1 - baseline] = client.Slots[id].Entity.Copy();
            }
        }

        private void ReadUpdate(uint id, Bitstream stream) {
            var entity = client.Slots[id].Entity;
            log.Debug(String.Format("{0} being updated as a {1}", id, entity.Class.ClassName));
            ReadAndUnpackFields(entity, stream);
        }

        private void Create(uint id, EntityClass clazz, uint baseline) {
            client.Created.Add(id);
            if (!client.Slots.ContainsKey(id)) {
                client.Slots[id] = new nora.lara.state.Client.Slot(null);
            }

            var slot = client.Slots[id];
            slot.Live = true;
            if (slot.Baselines[baseline] != null && slot.Baselines[baseline].Class.Equals(clazz)) {
                slot.Entity = slot.Baselines[baseline].Copy();
            } else {
                slot.Entity = Entity.CreateWith(id, clazz, client.FlatTables[(int) clazz.Id]);

                StringTable table = client.Strings[client.StringsIndex["instancebaseline"]];
                using (var stream = Bitstream.CreateWith(table.Get(clazz.Id.ToString()).Value)) {
                    ReadAndUnpackFields(slot.Entity, stream);
                }
            }

            foreach (var prop in slot.Entity.Properties) {
                var info = prop.Info;
                var handle = new Client.PropertyHandle() {
                    Entity = id,
                    Table = info.Origin.NetTableName,
                    Name = info.VarName
                };
                client.Properties[handle] = prop;
            }
        }

        private void ReadAndUnpackFields(Entity entity, Bitstream stream) {
            var fields = ReadFieldList(stream);

            foreach (uint field in fields) {
                entity.Properties[(int) field].Update(client.ClientTick, unpacker, stream);
            }
        }

        private List<uint> ReadFieldList(Bitstream stream) {
            var fields = new List<uint>();

            uint field = UInt32.MaxValue;
            field = ReadFieldNumber(field, stream);

            while (field != UInt32.MaxValue) {
                fields.Add(field);

                field = ReadFieldNumber(field, stream);
            }

            return fields;
        }

        private uint ReadFieldNumber(uint lastField, Bitstream stream) {
            if (stream.ReadBool()) {
                return unchecked(lastField + 1);
            } else {
                uint value = stream.ReadVarUInt();

                if (value == 0x3FFF) {
                    return UInt32.MaxValue;
                } else {
                    return unchecked(lastField + value + 1);
                }
            }
        }

        [FlagsAttribute]
        private enum UpdateFlag {

            None = 0,
            LeavePvs = 1 << 0,
            Delete = 1 << 1,
            EnterPvs = 1 << 2,
        }
    }
}
