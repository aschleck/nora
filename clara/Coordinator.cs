using nora.clara.machine;
using nora.machine;
using SteamKit2;
using SteamKit2.GC;
using SteamKit2.GC.Dota.Internal;
using SteamKit2.GC.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nora.clara {

    using log4net;
    using SteamKit2.Internal;

    public class Coordinator<S> where S : struct {

        private static readonly ILog log = LogManager.GetLogger(typeof(Coordinator<S>));

        private Machine<Event, S> Machine { get; set; }
        private Client Client { get; set; }

        public Coordinator(Machine<Event, S> machine, Client client, SteamBot bot) {
            this.Machine = machine;
            this.Client = client;

            bot.Client.AddHandler(new Handler<Coordinator<S>>(OnMessage));
            bot.CallbackManager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            bot.CallbackManager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
            bot.CallbackManager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedIn);
            bot.CallbackManager.Subscribe<SteamGameCoordinator.MessageCallback>(OnMessage);
        }

        private void OnMessage(IPacketMsg packetMsg) {
            Client.OnMessage(packetMsg);

            if (packetMsg.MsgType == EMsg.ClientPlayingSessionState) {
                var pb = new ClientMsgProtobuf<CMsgClientPlayingSessionState>(packetMsg);
                if (pb.Body.playing_app == 0) {
                    Machine.Trigger(Event.PLAYING_STATE_CLOSED);
                } else {
                    Machine.Trigger(Event.PLAYING_STATE_OPENED);
                }
            } else if (packetMsg.MsgType == EMsg.ClientGetAppOwnershipTicketResponse) {
                var pb = new ClientMsgProtobuf<CMsgClientGetAppOwnershipTicketResponse>(packetMsg);

                if (pb.Body.eresult != (uint) EResult.OK) {
                    throw new NotImplementedException("Unable to get app ticket");
                }

                Machine.Trigger(Event.GOT_APP_TICKET);
            } else if (packetMsg.MsgType == EMsg.ClientAuthListAck) {
                Machine.Trigger(Event.GOT_AUTH);
            } else if (packetMsg.MsgType == EMsg.ClientOGSBeginSessionResponse) {
                var msg = new ClientMsg<MsgClientOGSBeginSessionResponse>(packetMsg);

                if (msg.Body.Result != EResult.OK) {
                    throw new NotImplementedException("OGSBeginSession not OK");
                }

                Machine.Trigger(Event.GOT_SESSION);
            }
        }

        private void OnConnected(SteamClient.ConnectedCallback callback) {
        }

        private void OnDisconnected(SteamClient.DisconnectedCallback callback) {
            Machine.Trigger(Event.DISCONNECTED_FROM_STEAM);
        }

        private void OnLoggedIn(SteamUser.LoggedOnCallback callback) {
            if (callback.Result == EResult.OK) {
                Machine.Trigger(Event.CONNECTED_TO_STEAM);
            }
        }

        private void OnMessage(SteamGameCoordinator.MessageCallback callback) {
            CSODOTALobby was = Client.Lobby;
            Client.OnMessage(callback);
            CSODOTALobby now = Client.Lobby;

            if (callback.EMsg == (uint) EGCBaseClientMsg.k_EMsgGCClientWelcome) {
                var pb = new ClientGCMsgProtobuf<CMsgClientWelcome>(callback.Message);
                HandleWelcome(pb);
            } else if (callback.EMsg == (uint) EDOTAGCMsg.k_EMsgGCJoinChatChannelResponse) {
                Machine.Trigger(Event.JOINED_CHAT);
            } else if ((uint) EDOTAGCMsg.k_EMsgGCWatchGameResponse == callback.EMsg) {
                var pb = new ClientGCMsgProtobuf<CMsgWatchGameResponse>(callback.Message);
                HandleWatchResponse(pb);
            } else if (was == null && now != null) { // Probably due to Create
                Machine.Trigger(Event.CREATED_LOBBY);
            } else if (was != null && now == null) { // Probably due to CacheUnsubsribed.
                Machine.Trigger(Event.LEFT_LOBBY);
            } else if (was != null && now != null) {
                if (was.all_members.Count < now.all_members.Count) {
                    Machine.Trigger(Event.PLAYER_JOINED);
                } else if (was.all_members.Count > now.all_members.Count && now.all_members.Count == 1) {
                    Machine.Trigger(Event.EMPTIED);
                }

                if (was.state != now.state) {
                    if (now.state == CSODOTALobby.State.RUN) {
                        Machine.Trigger(Event.SERVER_RUNNING);
                    }
                }

                if (was.game_state != now.game_state) {
                    if (now.game_state == DOTA_GameState.DOTA_GAMERULES_STATE_WAIT_FOR_PLAYERS_TO_LOAD) {
                        Machine.Trigger(Event.SERVER_WAITING_FOR_PLAYERS);
                    }
                }

                int valid = now.all_members.Count((member) =>
                    member.team == DOTA_GC_TEAM.DOTA_GC_TEAM_BAD_GUYS ||
                    member.team == DOTA_GC_TEAM.DOTA_GC_TEAM_GOOD_GUYS);

                if (valid >= 3) {
                    Machine.Trigger(Event.LOBBY_READY);
                } else {
                    Machine.Trigger(Event.LOBBY_NOT_READY);
                }
            }
        }

        private void HandleWelcome(ClientGCMsgProtobuf<CMsgClientWelcome> pb) {
            if (Client.Lobby != null) {
                Machine.Trigger(Event.WELCOMED_STALE_LOBBY);
            } else {
                Machine.Trigger(Event.WELCOMED);
            }
        }

        private void HandleWatchResponse(ClientGCMsgProtobuf<CMsgWatchGameResponse> pb) {
            if (pb.Body.watch_game_result == CMsgWatchGameResponse.WatchGameResult.PENDING) {
                return;
            }

            switch (pb.Body.watch_game_result) {
                case CMsgWatchGameResponse.WatchGameResult.READY:
                    Machine.Trigger(Event.GOT_TV); break;
                case CMsgWatchGameResponse.WatchGameResult.GAMESERVERNOTFOUND:
                    Machine.Trigger(Event.DENIED_TV); break;
                case CMsgWatchGameResponse.WatchGameResult.LOBBYNOTFOUND:
                    Machine.Trigger(Event.DENIED_TV); break;
                case CMsgWatchGameResponse.WatchGameResult.MISSINGLEAGUESUBSCRIPTION:
                    Machine.Trigger(Event.DENIED_TV); break;
                default:
                    log.WarnFormat("Unknown watch game result {0}", pb.Body.watch_game_result);
                    break;
            }
        }
    }
}
