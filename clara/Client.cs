using log4net;
using nora.lara;
using ProtoBuf;
using ProtoBuf.Meta;
using SteamKit2;
using SteamKit2.GC;
using SteamKit2.GC.Dota.Internal;
using SteamKit2.GC.Internal;
using SteamKit2.Internal;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;

namespace nora.clara {

    public class Client {

        private static readonly ILog log = LogManager.GetLogger(typeof(Client));

        private static readonly int LOBBY_TYPE_ID = 2004;

        private static readonly Dictionary<int, Type> TYPES = new Dictionary<int, Type>() {
            { LOBBY_TYPE_ID, typeof(CSODOTALobby) }
        };

        public CSODOTAGameAccountClient Account { get; private set; }

        public CSODOTALobby Lobby {
            get {
                if (Active.ContainsKey(LOBBY_TYPE_ID)) {
                    return Objects[Active[LOBBY_TYPE_ID]][LOBBY_TYPE_ID] as CSODOTALobby;
                } else {
                    return null;
                }
            }
        }

        private SteamBot Bot { get; set; }
        private uint ClientVersion = 5988;

        private Dictionary<int, ulong> Active { get; set; }
        private Dictionary<string, ulong> Channels { get; set; }
        private Dictionary<ulong, Dictionary<int, object>> Objects { get; set; }

        public uint App { get; private set; }
        public ImmutableArray<byte> TicketForAuth { get; private set; }
        public ImmutableArray<byte> TicketForServer { get; private set; }
        public ulong SteamworksSessionId { get; private set; }
        public CMsgWatchGameResponse WatchResponse { get; private set; }

        private byte[] AppOwnershipTicket { get; set; }
        private ImmutableArray<byte> PendingTicketForAuth { get; set; }
        private uint AuthSequence;

        public Client(SteamBot bot) {
            this.Bot = bot;

            this.Active = new Dictionary<int, ulong>();
            this.Channels = new Dictionary<string, ulong>();
            this.Objects = new Dictionary<ulong, Dictionary<int, object>>();

            this.AuthSequence = 1;
        }

        public void Close() {
            Bot.CloseGame();
        }

        public void Launch(uint appId) {
            App = appId;
            
            var playing = new CMsgClientGamesPlayed.GamePlayed {
                game_id = appId,
                game_extra_info = "Dota 2",
                process_id = 6421,
                game_flags = 0,
//                owner_id = Bot.User.SteamID.AccountID
            };
            Bot.Play(playing);
        }

        public void SayHello() {
            Bot.SayHello(App);
        }

        public void AbandonCurrentGame() {
            var msg = new ClientGCMsgProtobuf<CMsgAbandonCurrentGame>(
                (uint) EDOTAGCMsg.k_EMsgGCAbandonCurrentGame);
            Bot.Coordinator.Send(msg, Games.DotaGameId);
        }

        public void Auth() {
            var pb = new CMsgAuthTicket();
            pb.gameid = App;
            pb.h_steam_pipe = 327684;

            using (var stream = Bitstream.CreateWith(PendingTicketForAuth.ToArray())) {
                pb.ticket_crc = CrcUtils.Compute32(stream);
            }

            pb.ticket = PendingTicketForAuth.ToArray();

            var msg = new ClientMsgProtobuf<CMsgClientAuthList>(EMsg.ClientAuthList);
            msg.Body.tokens_left = Bot.TokenCount;
            msg.Body.app_ids.Add(App);
            msg.Body.tickets.Add(pb);
            msg.Body.message_sequence = AuthSequence++;
            Bot.Client.Send(msg);
            log.Debug("Sent auth list with crc " + msg.Body.tickets[0].ticket_crc + "/" + msg.Body.message_sequence);
        }

        public void BeginGameServerSession() {
            TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var msg = new ClientMsg<MsgClientOGSBeginSession>();
            msg.Body.AccountId = Bot.Client.SteamID;
            msg.Body.AccountType = (byte) Bot.Client.SteamID.AccountType;
            msg.Body.AppId = App;
            msg.Body.TimeStarted = (uint) t.TotalSeconds;
            Bot.Client.Send(msg);
        }

        public void CreateAuthTicket() {
            PendingTicketForAuth = AuthTicket.CreateAuthTicket(Bot.NextToken(), Bot.PublicIP);
        }

        public void CreateLobby(string passKey)
        {

            var steam_id = 0u;
            //invite somebody to lobby
            if (steam_id != 0u)
            {
                var cmMsg = new ClientGCMsgProtobuf<CMsgInviteToParty>((uint)EGCBaseMsg.k_EMsgGCInviteToParty);
                cmMsg.Body.steam_id = steam_id;
                Bot.Coordinator.Send(cmMsg, 570);
                Thread.Sleep(5000);
            }
            
            
            var msg = new ClientGCMsgProtobuf<CMsgPracticeLobbyCreate>(
                (uint) EDOTAGCMsg.k_EMsgGCPracticeLobbyCreate);
            msg.Body.client_version = ClientVersion;
            msg.Body.pass_key = passKey;
            msg.Body.lobby_details = new CMsgPracticeLobbySetDetails();
            msg.Body.lobby_details.server_region = 8;
            msg.Body.lobby_details.allow_cheats = true;
            msg.Body.lobby_details.pass_key = passKey;
            msg.Body.lobby_details.game_name = "Test";
            Bot.Coordinator.Send(msg, Games.DotaGameId);
            
            Thread.Sleep(1000);
        }

        public void JoinLobbySlot(DOTA_GC_TEAM team) {
            var msg = new ClientGCMsgProtobuf<CMsgPracticeLobbySetTeamSlot>(
                (uint) EDOTAGCMsg.k_EMsgGCPracticeLobbySetTeamSlot);
            msg.Body.team = team;
            msg.Body.slot = 1;
            Bot.Coordinator.Send(msg, Games.DotaGameId);
        }

        public void GetAppTicket() {
            var msg = new ClientMsgProtobuf<CMsgClientGetAppOwnershipTicket>(
                EMsg.ClientGetAppOwnershipTicket);
            msg.Body.app_id = App;
            msg.SourceJobID = 1;
            Bot.Client.Send(msg);
        }

        public void JoinChat(string name, DOTAChatChannelType_t type) {
            var msg = new ClientGCMsgProtobuf<CMsgDOTAJoinChatChannel>(
                (uint) EDOTAGCMsg.k_EMsgGCJoinChatChannel);
            msg.Body.channel_name = name;
            msg.Body.channel_type = type;
            Bot.Coordinator.Send(msg, Games.DotaGameId);
        }

        public void LaunchLobby() {
            var msg = new ClientGCMsgProtobuf<CMsgPracticeLobbyLaunch>(
                (uint) EDOTAGCMsg.k_EMsgGCPracticeLobbyLaunch);
            msg.Body.client_version = ClientVersion;
            Bot.Coordinator.Send(msg, Games.DotaGameId);
        }

        public void LeaveChat(string name) {
            if (!Channels.ContainsKey(name)) {
                return;
            }

            var msg = new ClientGCMsgProtobuf<CMsgDOTALeaveChatChannel>(
                (uint) EDOTAGCMsg.k_EMsgGCLeaveChatChannel);
            msg.Body.channel_id = Channels[name];
            Bot.Coordinator.Send(msg, Games.DotaGameId);
            Channels.Remove(name);
        }

        public void LeaveLobby() {
            var lobby = new ClientGCMsgProtobuf<CMsgPracticeLobbyLeave>(
                (uint) EDOTAGCMsg.k_EMsgGCPracticeLobbyLeave);
            Bot.Coordinator.Send(lobby, Games.DotaGameId);
        }

        public void Watch(ulong serverSteamId) {
            var watch = new ClientGCMsgProtobuf<CMsgWatchGame>((uint) EDOTAGCMsg.k_EMsgGCWatchGame);
            watch.Body.client_version = ClientVersion;
            watch.Body.server_steamid = serverSteamId;
            Bot.Coordinator.Send(watch, Games.DotaGameId);
        }

        public void OnMessage(IPacketMsg packetMsg) {
            if (packetMsg.MsgType == EMsg.ClientGetAppOwnershipTicketResponse) {
                var pb = new ClientMsgProtobuf<CMsgClientGetAppOwnershipTicketResponse>(packetMsg);
                log.Debug("Got client ticket of " + pb.Body.app_id + " " + pb.Body.eresult + " " + pb.Body.ticket.Length);

                if (pb.Body.eresult == (uint) EResult.OK) {
                    AppOwnershipTicket = pb.Body.ticket;
                }
            } else if (packetMsg.MsgType == EMsg.ClientAuthListAck) {
                var pb = new ClientMsgProtobuf<CMsgClientAuthListAck>(packetMsg);
                log.Debug("Steam acked ticket crc of "
                    + pb.Body.ticket_crc 
                    + "/" 
                    + pb.Body.message_sequence);
                TicketForAuth = PendingTicketForAuth;
                TicketForServer = AuthTicket.CreateServerTicket(
                    Bot.Client.SteamID, TicketForAuth, AppOwnershipTicket);
            } else if (packetMsg.MsgType == EMsg.ClientTicketAuthComplete) {
                var pb = new ClientMsgProtobuf<CMsgClientTicketAuthComplete>(packetMsg);
                log.Debug("Client ticket auth complete with " 
                    + pb.Body.estate 
                    + " on " 
                    + pb.Body.ticket_crc);
            } else if (packetMsg.MsgType == EMsg.ClientOGSBeginSessionResponse) {
                var msg = new ClientMsg<MsgClientOGSBeginSessionResponse>(packetMsg);

                if (msg.Body.Result == EResult.OK) {
                    SteamworksSessionId = msg.Body.SessionId;
                }
            }
        }

        public void OnMessage(SteamGameCoordinator.MessageCallback callback) {
            if ((uint) EGCBaseClientMsg.k_EMsgGCClientWelcome == callback.EMsg) {
                var pb = new ClientGCMsgProtobuf<CMsgClientWelcome>(callback.Message);

                if (ClientVersion != pb.Body.version) {
                    log.WarnFormat(
                        "Version mismatch, Clara ver {0} but GC ver {1}",
                        ClientVersion,
                        pb.Body.version);
                    ClientVersion = pb.Body.version;
                }

                log.DebugFormat("Welcomed to version {0}", pb.Body.version);
                foreach (var cache in pb.Body.outofdate_subscribed_caches) {
                    foreach (var obj in cache.objects) {
                        // TODO: Multiple objects???
                        Set(cache.owner_soid.id, obj.type_id, obj.object_data[0]);
                    }
                }

                using (var stream = new MemoryStream(pb.Body.game_data)) {
                    Account = Serializer.Deserialize<CSODOTAGameAccountClient>(stream);
                }
            } else if (callback.EMsg == (uint) EDOTAGCMsg.k_EMsgGCJoinChatChannelResponse) {
                var pb = new ClientGCMsgProtobuf<CMsgDOTAJoinChatChannelResponse>(callback.Message);
                Channels[pb.Body.channel_name] = pb.Body.channel_id;
            } else if ((uint) EDOTAGCMsg.k_EMsgGCWatchGameResponse == callback.EMsg) {
                var pb = new ClientGCMsgProtobuf<CMsgWatchGameResponse>(callback.Message);
                WatchResponse = pb.Body;
            } else if ((uint) ESOMsg.k_ESOMsg_Create == callback.EMsg) {
                var pb = new ClientGCMsgProtobuf<CMsgSOSingleObject>(callback.Message);
                Set(pb.Body.owner_soid.id, pb.Body.type_id, pb.Body.object_data);
            } else if ((uint) ESOMsg.k_ESOMsg_Update == callback.EMsg) {
                var pb = new ClientGCMsgProtobuf<CMsgSOSingleObject>(callback.Message);
                Set(pb.Body.owner_soid.id, pb.Body.type_id, pb.Body.object_data);
            } else if ((uint) ESOMsg.k_ESOMsg_UpdateMultiple == callback.EMsg) {
                var pb = new ClientGCMsgProtobuf<CMsgSOMultipleObjects>(callback.Message);

                foreach (var obj in pb.Body.objects_modified) {
                    Set(pb.Body.owner_soid.id, obj.type_id, obj.object_data);
                }
            } else if ((uint) ESOMsg.k_ESOMsg_CacheUnsubscribed == callback.EMsg) {
                var pb = new ClientGCMsgProtobuf<CMsgSOCacheUnsubscribed>(callback.Message);

                Objects.Remove(pb.Body.owner_soid.id);
                Active = Active.Where(pair => pair.Value != pb.Body.owner_soid.id)
                               .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
        }

        private Dictionary<int, object> Get(ulong owner) {
            if (!Objects.ContainsKey(owner)) {
                Objects[owner] = new Dictionary<int, object>();
            }
            return Objects[owner];
        }

        private void Set(ulong ownerId, int typeId, byte[] data) {
            Dictionary<int, object> owner = Get(ownerId);
            owner[typeId] = Deserialize(typeId, data);
            Active[typeId] = ownerId;
        }

        private object Deserialize(int typeId, byte[] data) {
            if (!TYPES.ContainsKey(typeId)) {
                return null;
            }

            if (typeId == LOBBY_TYPE_ID) {
                log.Debug("Updated lobby");
            }

            Type type = TYPES[typeId];
            using (var stream = new MemoryStream(data)) {
                return RuntimeTypeModel.Default.Deserialize(stream, null, type);
            }
        }
    }
}
