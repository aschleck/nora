using log4net;
using nora.lara.machine;
using nora.lara.net;
using nora.lara.state;
using nora.lara.unpackers;
using nora.protos;
using ProtoBuf;
using System;

namespace nora.lara {

    public class SignonProcessor : Processor {

        private static ILog log = LogManager.GetLogger(typeof(SignonProcessor));

        private Client client;
        private Connection connection;
        private EntityUpdater entityUpdater;
        private SendTableFlattener sendTableFlattener;
        private StringTableUpdater stringTableUpdater;

        public SignonProcessor(Client client, Connection connection) {
            this.client = client;
            this.connection = connection;
            this.entityUpdater = new EntityUpdater(client);
            this.sendTableFlattener = new SendTableFlattener();
            this.stringTableUpdater = new StringTableUpdater();
        }

        public void EnterConnected() {
            connection.OpenChannel();
            client.Reset();

            var scv = new CNETMsg_SetConVar();
            scv.convars = client.ExposeCVars();

            var scvMessage = Connection.ConvertProtoToMessage<CNETMsg_SetConVar>(
                (uint) NET_Messages.net_SetConVar,
                scv);

            var ss = new CNETMsg_SignonState();
            ss.num_server_players = 0;
            ss.spawn_count = 0xFFFFFFFF;
            ss.signon_state = (uint) SIGNONSTATE.SIGNONSTATE_CONNECTED;

            var ssMessage = Connection.ConvertProtoToMessage<CNETMsg_SignonState>(
                (uint) NET_Messages.net_SignonState,
                ss);

            connection.SendReliably(scvMessage, ssMessage);
        }

        public void EnterNew() {
            var ci = new CCLCMsg_ClientInfo();
            ci.server_count = client.ServerCount;
            var ciMessage = Connection.ConvertProtoToMessage<CCLCMsg_ClientInfo>(
                (uint) CLC_Messages.clc_ClientInfo,
                ci);

            var ss = new CNETMsg_SignonState();
            ss.signon_state = (uint) SIGNONSTATE.SIGNONSTATE_NEW;
            ss.spawn_count = client.ServerCount;
            ss.num_server_players = 0;

            var ssMessage = Connection.ConvertProtoToMessage<CNETMsg_SignonState>(
                (uint) NET_Messages.net_SignonState,
                ss);

            var scv = new CNETMsg_SetConVar();
            scv.convars = new CMsg_CVars();
            var cvar = new CMsg_CVars.CVar();
            cvar.name = "steamworks_sessionid_client";
            cvar.value = client.SteamworksSessionId.ToString();
            client.CVars["steamworks_sessionid_client"] = client.SteamworksSessionId.ToString();
            scv.convars.cvars.Add(cvar);

            var scvMessage = Connection.ConvertProtoToMessage<CNETMsg_SetConVar>(
                (uint) NET_Messages.net_SetConVar,
                scv);

            connection.SendReliably(ciMessage, ssMessage, scvMessage);
        }

        public void EnterPrespawn() {
            var ss = new CNETMsg_SignonState();
            ss.signon_state = (uint) SIGNONSTATE.SIGNONSTATE_PRESPAWN;
            ss.spawn_count = client.ServerCount;
            ss.num_server_players = 0;

            var ssMessage = Connection.ConvertProtoToMessage<CNETMsg_SignonState>(
                (uint) NET_Messages.net_SignonState,
                ss);

            // Send mask for game events? 
            // 0c 23
            //   0d 8b820592
            //   0d 0140e890
            //   0d f6ffff7f
            //   0d ff9bfc6e
            //   0d 0310e87c
            //   0d cbfffff8
            //   0d effc0700
            var le = new CCLCMsg_ListenEvents();
            for (uint i = 0; i < 267; i += 32) {
                le.event_mask.Add(0xffffffff);
            }
            le.event_mask.Add(0x0000000a);
            var leMessage = Connection.ConvertProtoToMessage<CCLCMsg_ListenEvents>(
                (uint) CLC_Messages.clc_ListenEvents,
                le);

            connection.SendReliably(ssMessage, leMessage);
        }

        public void EnterSpawn() {
            var ss = new CNETMsg_SignonState();
            ss.signon_state = (uint) SIGNONSTATE.SIGNONSTATE_SPAWN;
            ss.spawn_count = client.ServerCount;
            ss.num_server_players = 0;

            var ssMessage = Connection.ConvertProtoToMessage<CNETMsg_SignonState>(
                (uint) NET_Messages.net_SignonState,
                ss);
            connection.SendReliably(ssMessage);
        }

        public Event? Process(byte[] message) {
            log.Warn("Ignoring message " + message + " " + message[0]);
            return null;
        }

        public Event? Process(Connection.Message message) {
            using (var stream = Bitstream.CreateWith(message.Data)) {
                if (message.Type == (uint) NET_Messages.net_NOP) {
                    return null;
                } else if (message.Type == (uint) NET_Messages.net_Disconnect) {
                    return Process(Serializer.Deserialize<CNETMsg_Disconnect>(stream));
                } else if (message.Type == (uint) NET_Messages.net_Tick) {
                    return Process(Serializer.Deserialize<CNETMsg_Tick>(stream));
                } else if (message.Type == (uint) NET_Messages.net_SetConVar) {
                    return Process(Serializer.Deserialize<CNETMsg_SetConVar>(stream));
                } else if (message.Type == (uint) NET_Messages.net_SignonState) {
                    return Process(Serializer.Deserialize<CNETMsg_SignonState>(stream));
                } else if (message.Type == (uint) SVC_Messages.svc_ServerInfo) {
                    return Process(Serializer.Deserialize<CSVCMsg_ServerInfo>(stream));
                } else if (message.Type == (uint) SVC_Messages.svc_SendTable) {
                    return Process(Serializer.Deserialize<CSVCMsg_SendTable>(stream));
                } else if (message.Type == (uint) SVC_Messages.svc_ClassInfo) {
                    return Process(Serializer.Deserialize<CSVCMsg_ClassInfo>(stream));
                } else if (message.Type == (uint) SVC_Messages.svc_PacketEntities) {
                    return Process(Serializer.Deserialize<CSVCMsg_PacketEntities>(stream));
                } else if (message.Type == (uint) SVC_Messages.svc_CreateStringTable) {
                    return Process(Serializer.Deserialize<CSVCMsg_CreateStringTable>(stream));
                } else if (message.Type == (uint) SVC_Messages.svc_UpdateStringTable) {
                    return Process(Serializer.Deserialize<CSVCMsg_UpdateStringTable>(stream));
                } else if (message.Type == (uint) SVC_Messages.svc_Print) {
                    return Process(Serializer.Deserialize<CSVCMsg_Print>(stream));
                } else if (message.Type == (uint) SVC_Messages.svc_GameEventList) {
                    return Process(Serializer.Deserialize<CSVCMsg_GameEventList>(stream));
                } else {
                    log.Warn("Unknown message " + message.Type);
                    return null;
                }
            }
        }

        private Event? Process(CNETMsg_Disconnect message) {
            log.Debug(String.Format("CNETMsg_Disconnect: {0}", message.reason));

            return Event.DISCONNECTED;
        }

        private Event? Process(CNETMsg_Tick message) {
            log.Debug(String.Format("CNETMsg_Tick: tick {0}", message.tick));

            if (message.tick > client.ClientTick) {
                client.ClientTick = message.tick;
            }

            client.ServerTick = message.tick;

            return null;
        }

        private Event? Process(CNETMsg_SetConVar message) {
            log.Debug(String.Format("CNETMsg_SetConVar: cvars {0}", message.convars.cvars.Count));

            foreach (var cvar in message.convars.cvars) {
                client.CVars[cvar.name] = cvar.value;

                log.Debug(String.Format("Set cvar {0} to {1}", cvar.name, cvar.value));
            }

            return null;
        }

        private Event? Process(CNETMsg_SignonState message) {
            log.Debug(String.Format("CNETMsg_SignonState: {0}", message.signon_state));

            switch (message.signon_state) {
                case (uint) SIGNONSTATE.SIGNONSTATE_CONNECTED: return Event.GO_CONNECTED;
                case (uint) SIGNONSTATE.SIGNONSTATE_NEW: return Event.GO_NEW;
                case (uint) SIGNONSTATE.SIGNONSTATE_PRESPAWN: return Event.GO_PRESPAWN;
                case (uint) SIGNONSTATE.SIGNONSTATE_SPAWN: return Event.GO_SPAWN;
                default: throw new NotImplementedException("Unknown signon state " + message.signon_state);
            }
        }

        private Event? Process(CSVCMsg_ServerInfo message) {
            client.ServerCount = (uint) message.server_count;
            client.TickInterval = message.tick_interval;

            log.Debug(String.Format("CSVCMsg_ServerInfo: {0}", message.map_name));
            return null;
        }

        private Event? Process(CSVCMsg_SendTable message) {
            client.SendTables.Add(SendTable.CreateWith(message));

            log.Debug(String.Format("CSVCMsg_SendTable: {0} with {1} props",
                message.net_table_name, message.props.Count));

            return null;
        }

        private Event? Process(CSVCMsg_ClassInfo message) {
            foreach (var clazz in message.classes) {
                var created = EntityClass.CreateWith(clazz);
                client.Classes.Add(created);
                client.ClassesByName.Add(created.ClassName, created);
            }

            foreach (var table in client.SendTables) {
                for (int i = 0; i < table.Properties.Count; ++i) {
                    var prop = table.Properties[i];

                    if (prop.Type == PropertyInfo.PropertyType.Array) {
                        prop.ArrayProp = table.Properties[i - 1];
                    }
                }
            }

            client.FlatTables.AddRange(sendTableFlattener.Flatten(client.SendTables));

            log.Debug(String.Format("CSVCMsg_ClassInfo: create_on_client {0} with {1} classes",
                message.create_on_client, message.classes.Count));

            return null;
        }

        private Event? Process(CSVCMsg_PacketEntities message) {
            log.Debug("svc_PacketEntities is_delta: "
                + message.is_delta
                + " baseline: " + message.baseline
                + " update_baseline: " + message.update_baseline
                + " delta: " + message.delta_from);

            using (var stream = Bitstream.CreateWith(message.entity_data)) {
                entityUpdater.Update(
                    stream,
                    (uint) message.baseline,
                    message.update_baseline,
                    (uint) message.updated_entries,
                    message.is_delta);
            }

            if (message.update_baseline) {
                client.Baseline = message.baseline;
                return Event.BASELINE;
            } else {
                return null;
            }
        }

        private Event? Process(CSVCMsg_CreateStringTable message) {
            var table = StringTable.Create(message);
            client.StringsIndex[message.name] = client.Strings.Count;
            client.Strings.Add(table);

            stringTableUpdater.Update(table, message.num_entries, message.string_data);

            log.Debug(String.Format(
                "CSVCMsg_CreateStringTable: name {0} with {1} bytes of data",
                message.name, message.string_data.Length));
            return null;
        }

        private Event? Process(CSVCMsg_UpdateStringTable message) {
            StringTable table = client.Strings[message.table_id];

            stringTableUpdater.Update(table, message.num_changed_entries, message.string_data);

            log.Debug(String.Format(
                "CSVCMsg_UpdateStringTable: id {0} with {1} bytes of data",
                message.table_id, message.string_data.Length));
            return null;
        }

        private Event? Process(CSVCMsg_Print message) {
            log.Debug(String.Format("CSVCMsg_Print: {0}", message.text));
            return null;
        }

        private Event? Process(CSVCMsg_GameEventList message) {
            log.Debug("CSVCMsg_GameEventList:");
            foreach (var descriptor in message.descriptors) {
                log.DebugFormat(
                    "  id: {0} name: {1}", 
                    descriptor.eventid, descriptor.name);
                foreach (var key in descriptor.keys) {
                    log.DebugFormat("    name: {0} type: {1}", key.name, key.type);
                }
            }
            return null;
        }
    }
}
