using nora.lara.state;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nora.lara.unpackers {

    public class SendTableFlattener {

        public List<FlatTable> Flatten(List<SendTable> sendTables) {
            List<FlatTable> flattened = new List<FlatTable>();

            Dictionary<string, SendTable> nameMap = 
                sendTables.ToDictionary(t => t.NetTableName, t => t);

            foreach (var table in sendTables) {
                var excluding = new HashSet<string>();
                GatherExcludes(table, nameMap, excluding);

                List<PropertyInfo> properties = new List<PropertyInfo>();
                BuildHierarchy(properties, table, nameMap, excluding);
                SortProperties(properties);

                flattened.Add(
                    FlatTable.CreateWith(table.NetTableName, table.NeedsDecoder, properties));
            }

            return flattened;
        }

        private void GatherExcludes(
                SendTable table,
                Dictionary<string, SendTable> all,
                HashSet<string> excluding) {
            foreach (var property in table.Properties) {
                if (property.Flags.HasFlag(PropertyInfo.MultiFlag.Exclude)) {
                    excluding.Add(QualifyProperty(property.DtName, property));
                } else if (property.Type == PropertyInfo.PropertyType.DataTable) {
                    GatherExcludes(all[property.DtName], all, excluding);
                }
            }
        }

        private void BuildHierarchy(
                List<PropertyInfo> properties,
                SendTable send, 
                Dictionary<string, SendTable> all,
                HashSet<string> excluding) {
            var nonDtProps = new List<PropertyInfo>();
            GatherProperties(properties, send, all, nonDtProps, excluding);

            properties.AddRange(nonDtProps);
        }

        private void GatherProperties(
                List<PropertyInfo> properties, 
                SendTable send, 
                Dictionary<string, SendTable> all,
                List<PropertyInfo> nonDtProps, 
                HashSet<string> excluding) {
            var skipOn = PropertyInfo.MultiFlag.Exclude | PropertyInfo.MultiFlag.InsideArray;

            foreach (PropertyInfo property in send.Properties) {
                if ((uint) (property.Flags & skipOn) > 0) {
                    continue;
                } else if (excluding.Contains(QualifyProperty(property.Origin.NetTableName, property))) {
                    continue;
                }

                if (property.Type == PropertyInfo.PropertyType.DataTable) {
                    var pointsAt = all[property.DtName];

                    if (property.Flags.HasFlag(PropertyInfo.MultiFlag.Collapsible)) {
                        GatherProperties(properties, pointsAt, all, nonDtProps, excluding);
                    } else {
                        BuildHierarchy(properties, pointsAt, all, excluding);
                    }
                } else {
                    nonDtProps.Add(property);
                }
            }
        }

        private void SortProperties(List<PropertyInfo> properties) {
            var priorities = new List<uint>();
            priorities.Add(64);

            foreach (var property in properties) {
                if (!priorities.Contains(property.Priority)) {
                    priorities.Add(property.Priority);
                }
            }

            priorities.Sort();

            int offset = 0;
            foreach (uint priority in priorities) {
                int hole = offset;
                int cursor = hole;

                while (cursor < properties.Count) {
                    var prop = properties[cursor];

                    bool cutLine = priority == 64 &&
                        prop.Flags.HasFlag(PropertyInfo.MultiFlag.ChangesOften);
                    if (prop.Priority == priority || cutLine) {
                        properties[cursor] = properties[hole];
                        properties[hole] = prop;

                        ++hole;
                        ++offset;
                    }

                    ++cursor;
                }
            }
        }

        private string QualifyProperty(string table, PropertyInfo property) {
            return String.Format("{0}.{1}", table, property.VarName);
        }
    }
}
