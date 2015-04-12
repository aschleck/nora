using log4net;
using nora.lara.machine;
using nora.lara.net;
using nora.lara.state;
using nora.lara.unpackers;
using nora.protos;
using ProtoBuf;
using System;
using System.Collections.Generic;

namespace nora.lara {

    public class GameProcessor : Processor {
        
        private static ILog log = LogManager.GetLogger(typeof(GameProcessor));

        private readonly Client client;
        private readonly Connection connection;
        private readonly EntityUpdater entityUpdater;
        private readonly StringTableUpdater stringTableUpdater;

        public GameProcessor(Client client, Connection connection) {
            this.client = client;
            this.connection = connection;
            this.entityUpdater = new EntityUpdater(client);
            this.stringTableUpdater = new StringTableUpdater();
        }

        public void EnterGame() {
            var ack = new CCLCMsg_BaselineAck();
            ack.baseline_nr = client.Baseline;
            ack.baseline_tick = (int) client.ServerTick;
            var ackMsg = Connection.ConvertProtoToMessage<CCLCMsg_BaselineAck>(
                (uint) CLC_Messages.clc_BaselineAck,
                ack);

            var ss = new CNETMsg_SignonState();
            ss.num_server_players = 0;
            ss.signon_state = (uint) SIGNONSTATE.SIGNONSTATE_FULL;
            ss.spawn_count = client.ServerCount;

            var ssMessage = Connection.ConvertProtoToMessage<CNETMsg_SignonState>(
                (uint) NET_Messages.net_SignonState,
                ss);

            var le = new CCLCMsg_ListenEvents();
            for (uint i = 0; i < 267; i += 32) {
                le.event_mask.Add(0xffffffff);
            }
            le.event_mask.Add(0x000003ff);
            var leMessage = Connection.ConvertProtoToMessage<CCLCMsg_ListenEvents>(
                (uint) CLC_Messages.clc_ListenEvents,
                le);

            connection.SendReliably(ackMsg, ssMessage, leMessage);

            List<Connection.Message> clientMsgs = new List<Connection.Message>();
            for (int i = 0; i < 10; ++i) {
                var msg = new CCLCMsg_ClientMessage();
                msg.data = new byte[] { 0x0D, 0xCD, 0xCC, 0xCC, 0x3F };
                msg.msg_type = 2;

                var msgMessage = Connection.ConvertProtoToMessage<CCLCMsg_ClientMessage>(
                    (uint) CLC_Messages.clc_ClientMessage,
                    msg);
                clientMsgs.Add(msgMessage);
            }
            connection.SendUnreliably(clientMsgs.ToArray());
        }

        public Event? Process(Connection.Message message) {
            using (var stream = Bitstream.CreateWith(message.Data)) {
                if (message.Type == (uint) NET_Messages.net_NOP) {
                    return null;
                } else if (message.Type == (uint) NET_Messages.net_Disconnect) {
                    return Process(Serializer.Deserialize<CNETMsg_Disconnect>(stream));
                } else if (message.Type == (uint) NET_Messages.net_StringCmd) {
                    return Process(Serializer.Deserialize<CNETMsg_StringCmd>(stream));
                } else if (message.Type == (uint) NET_Messages.net_Tick) {
                    return Process(Serializer.Deserialize<CNETMsg_Tick>(stream));
                } else if (message.Type == (uint) SVC_Messages.svc_PacketEntities) {
                    return Process(Serializer.Deserialize<CSVCMsg_PacketEntities>(stream));
                } else if (message.Type == (uint) SVC_Messages.svc_UpdateStringTable) {
                    return Process(Serializer.Deserialize<CSVCMsg_UpdateStringTable>(stream));
                } else if (message.Type == (uint) SVC_Messages.svc_UserMessage) {
                    return Process(Serializer.Deserialize<CSVCMsg_UserMessage>(stream));
                } else if (message.Type == (uint) SVC_Messages.svc_GameEvent) {
                    return Process(Serializer.Deserialize<CSVCMsg_GameEvent>(stream));
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

        private Event? Process(CNETMsg_StringCmd message) {
            log.Debug(String.Format("CNETMsg_StringCmd: {0}", message.command));

            return null;
        }

        private Event? Process(CNETMsg_Tick message) {
            log.Debug(String.Format("CNETMsg_Tick: tick {0}", message.tick));

            if (message.tick > client.ClientTick) {
                client.ClientTick = message.tick;
            }

            client.ServerTick = message.tick;

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
                var ack = new CCLCMsg_BaselineAck();
                ack.baseline_nr = client.Baseline;
                ack.baseline_tick = (int) client.ServerTick;
                var ackMsg = Connection.ConvertProtoToMessage<CCLCMsg_BaselineAck>(
                    (uint) CLC_Messages.clc_BaselineAck,
                    ack);
                connection.SendReliably(ackMsg);
                return null;
            } else {
                return null;
            }
        }

        private Event? Process(CSVCMsg_UpdateStringTable message) {
            StringTable table = client.Strings[message.table_id];

            stringTableUpdater.Update(table, message.num_changed_entries, message.string_data);

            log.Debug(String.Format(
                "CSVCMsg_UpdateStringTable: id {0} with {1} bytes of data",
                message.table_id, message.string_data.Length));
            return null;
        }

        private Event? Process(CSVCMsg_UserMessage message) {
            if (message.msg_type == (int) EBaseUserMessages.UM_SayText2) {
                using (var stream = Bitstream.CreateWith(message.msg_data)) {
                    return Process(Serializer.Deserialize<CUserMsg_SayText2>(stream));
                }
            } else if (message.msg_type == (int) EDotaUserMessages.DOTA_UM_ChatEvent) {
                using (var stream = Bitstream.CreateWith(message.msg_data)) {
                    return Process(Serializer.Deserialize<CDOTAUserMsg_ChatEvent>(stream));
                }
            } else {
                log.DebugFormat("CSVCMsg_UserMessage: unknown type {0} with {1} bytes of data",
                    message.msg_type, message.msg_data.Length);
                return null;
            }
        }

        private Event? Process(CUserMsg_SayText2 message) {
            log.DebugFormat("CUserMsg_SayText2: {0} on {1} says {2}",
                message.prefix, message.format, message.text);
            return null;
        }

        private Event? Process(CDOTAUserMsg_ChatEvent message) {
            log.DebugFormat("CSVCMsg_ChatEvent: type {0} with value {1}",
                message.type, message.value);
            return null;
        }

        private Event? Process(CSVCMsg_GameEvent message) {
            log.DebugFormat("CSVCMsg_GameEvent: name {0} id {1}",
                message.event_name, message.eventid);
            return null;
        }

        public Event? Process(byte[] message) {
            throw new NotImplementedException();
        }
    }
}
