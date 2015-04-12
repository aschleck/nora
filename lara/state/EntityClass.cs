using nora.protos;

namespace nora.lara.state {

    public struct EntityClass {

        public static EntityClass CreateWith(CSVCMsg_ClassInfo.class_t proto) {
            return new EntityClass() {
                Id = (uint) proto.class_id,
                DataTableName = proto.data_table_name,
                ClassName = proto.class_name,
            };
        }

        public uint Id { get; private set; }
        public string DataTableName { get; private set; }
        public string ClassName { get; private set; }

        public override bool Equals(object obj) {
            if (!(obj is EntityClass)) {
                return false;
            }

            var o = (EntityClass) obj;
            return o.Id == Id;
        }
    }
}
