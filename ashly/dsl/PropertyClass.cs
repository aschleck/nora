using nora.lara.state;
using System;
using System.Collections.Generic;

namespace nora.ashly.dsl {

    public class PropertyClass {

        public readonly uint Id;
        private readonly Client client;

        protected PropertyClass(uint id, Client client) {
            this.Id = id;
            this.client = client;
        }

        protected EnumValue<T> bindE<T>(string table, string name) where T : IConvertible {
            return new EnumValue<T>(client.Properties[new Client.PropertyHandle() {
                Entity = Id,
                Table = table,
                Name = name,
            }]);
        }

        protected TypedValue<T> bind<T>(string table, string name) {
            return new TypedValue<T>(client.Properties[new Client.PropertyHandle() {
                Entity = Id,
                Table = table,
                Name = name,
            }]);
        }

        protected Handle handle(string table, string name) {
            return new Handle(
                client.Properties[new Client.PropertyHandle() {
                    Entity = Id,
                    Table = table,
                    Name = name,
                }],
                client);
        }

        protected RangeValue<T> range<T>(string name, int count) {
            List<Property> properties = new List<Property>();
            for (int i = 0; i < count; ++i) {
                properties.Add(client.Properties[new Client.PropertyHandle() {
                    Entity = Id,
                    Table = name,
                    Name = i.ToString("D4"),
                }]);
            }
            return new RangeValue<T>(properties);
        }
    }
}
