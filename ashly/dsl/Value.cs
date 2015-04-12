using nora.lara.state;
using System;
using System.Collections.Generic;

namespace nora.ashly.dsl {

    public interface Value {

        uint? ReadAt { get; }
    }

    public class Handle : Value {

        private readonly Property property;
        private readonly Client client;
        private ushort value;

        public uint? ReadAt { get; private set; }
        public ushort? Value {
            get {
                if (!ReadAt.HasValue || ReadAt.Value < property.UpdatedAt) {
                    ReadAt = property.UpdatedAt;
                    value = (ushort) (property.ValueAs<uint>() & 0x7FF);
                }
                if (client.Slots.ContainsKey(value)) {
                    var slot = client.Slots[value];
                    if (slot.Live) {
                        return value;
                    }
                }

                return null;
            }
        }

        public Handle(Property property, Client client) {
            this.property = property;
            this.client = client;
            this.value = 0;
            this.ReadAt = null;
        }
    }

    public class RangeValue<T> : Value {

        private readonly List<Property> properties;
        private uint?[] readAt;
        private T[] values;

        public uint? ReadAt { get; private set; }
        public T[] Value {
            get {
                uint latest = 0;
                for (int i = 0; i < properties.Count; ++i) {
                    var property = properties[i];
                    latest = Math.Max(latest, property.UpdatedAt);
                    if (!readAt[i].HasValue || readAt[i].Value < property.UpdatedAt) {
                        readAt[i] = property.UpdatedAt;
                        values[i] = property.ValueAs<T>();
                    }
                }
                ReadAt = latest;
                return values;
            }
        }

        public RangeValue(List<Property> properties) {
            this.properties = properties;
            this.readAt = new uint?[properties.Count];
            this.values = new T[properties.Count];
        }
    }

    public class EnumValue<T> : TypedValue<uint> where T : IConvertible {

        public new T Value {
            get {
                return (T) Enum.ToObject(typeof(T), base.Value);
            }
        }

        public EnumValue(Property property) : base(property) {
        }
    }

    public class TypedValue<T> : Value {

        private readonly Property property;
        private T value;

        public uint? ReadAt { get; private set; }
        public T Value {
            get {
                if (!ReadAt.HasValue || ReadAt.Value < property.UpdatedAt) {
                    ReadAt = property.UpdatedAt;
                    value = property.ValueAs<T>();
                }
                return value;
            }
        }

        public TypedValue(Property property) {
            this.property = property;
            this.value = default(T);
            this.ReadAt = null;
        }
    }
}
