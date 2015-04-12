using nora.ashly.dsl;
using nora.lara.state;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace nora.ashly {

    class Zoo {

        private readonly Client client;
        private readonly ImmutableDictionary<EntityClass, Constructor> constructors;
        private readonly Dictionary<uint, PropertyClass> entities;
        private readonly Dictionary<Type, HashSet<uint>> entitiesByType;

        private Zoo(Client client, ImmutableDictionary<EntityClass, Constructor> constructors) {
            this.client = client;
            this.constructors = constructors;
            this.entities = new Dictionary<uint, PropertyClass>();
            this.entitiesByType = new Dictionary<Type, HashSet<uint>>();

            foreach (var constructor in constructors.Values) {
                this.entitiesByType.Add(constructor.type, new HashSet<uint>());
            }
        }

        public ImmutableHashSet<T> Get<T>() where T : PropertyClass {
            return entitiesByType[typeof(T)]
                .Select((i) => (T) entities[i])
                .ToImmutableHashSet();
        }

        public T GetSingle<T>() where T : PropertyClass {
            return (T) entities[entitiesByType[typeof(T)].Single()];
        }

        public T Get<T>(uint id) where T : PropertyClass {
            return (T) entities[id];
        }

        public bool Has<T>() {
            return entitiesByType[typeof(T)].Count > 0;
        }

        public void Tick() {
            foreach (var deleted in client.Deleted) {
                if (entities.ContainsKey(deleted)) {
                    entitiesByType[entities[deleted].GetType()].Remove(deleted);
                    entities.Remove(deleted);
                }
            }

            foreach (var created in client.Created) {
                var entity = client.Slots[created].Entity;
                if (constructors.ContainsKey(entity.Class)) {
                    var constructor = constructors[entity.Class];
                    entities[created] = constructor.factory(created, client);
                    entitiesByType[constructor.type].Add(created);
                }
            }
        }

        private struct Constructor {

            public Type type;
            public Func<uint, Client, PropertyClass> factory;
        }

        public class Builder {

            private readonly Client client;
            private readonly ImmutableDictionary<EntityClass, Constructor>.Builder constructors;

            public Builder(Client client) {
                this.client = client;
                this.constructors = 
                    ImmutableDictionary.CreateBuilder<EntityClass, Constructor>();
            }

            public Builder Associate<T>(
                    EntityClass clazz, 
                    Func<uint, Client, PropertyClass> factory) {
                this.constructors.Add(clazz, new Constructor() {
                    type = typeof(T),
                    factory = factory,
                });
                return this;
            }

            public Zoo Build() {
                return new Zoo(client, constructors.ToImmutable());
            }
        }
    }
}
