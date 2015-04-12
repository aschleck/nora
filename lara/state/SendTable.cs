using nora.protos;
using System.Collections.Generic;

namespace nora.lara.state {

    public class SendTable {

        public static SendTable CreateWith(CSVCMsg_SendTable proto) {
            var table = new SendTable() {
                NetTableName = proto.net_table_name,
                NeedsDecoder = proto.needs_decoder,
            };

            foreach (var prop in proto.props) {
                table.Properties.Add(PropertyInfo.CreateWith(prop, table));
            }

            return table;
        }

        public string NetTableName { get; private set; }
        public bool NeedsDecoder { get; private set; }
        public List<PropertyInfo> Properties { get; private set; }

        private SendTable() {
            this.Properties = new List<PropertyInfo>();
        }
    }
}
