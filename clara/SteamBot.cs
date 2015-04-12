using log4net;
using nora.lara;
using SteamKit2;
using SteamKit2.GC;
using SteamKit2.GC.Internal;
using SteamKit2.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Net;
using System.Threading;
using System.Collections.Immutable;
using System.Linq;

namespace nora.clara {

    public class SteamBot {

        public static SteamBot Create(string username, string password) {
            SteamBot clara = new SteamBot(username, password);

            clara.Client.AddHandler(new Handler<SteamBot>(clara.OnMessage));
            new Callback<SteamClient.ConnectedCallback>(clara.onConnected, clara.CallbackManager);
            new Callback<SteamClient.DisconnectedCallback>(clara.onDisconnected, clara.CallbackManager);
            new Callback<SteamUser.LoggedOnCallback>(clara.onLoggedIn, clara.CallbackManager);
            new Callback<SteamApps.AppOwnershipTicketCallback>(clara.onAppOwnership, clara.CallbackManager);
            new Callback<SteamApps.GameConnectTokensCallback>(clara.onTokensCallback, clara.CallbackManager);

            new Thread(clara.Run).Start();

            return clara;
        }

        private static readonly ILog log = LogManager.GetLogger(typeof(SteamBot));

        public SteamUser.LogOnDetails Logon { get; private set; }

        public SteamClient Client { get; private set; }
        public SteamGameCoordinator Coordinator { get; private set; }
        public SteamFriends Friends { get; private set; }
        public SteamUser User;
        public CallbackManager CallbackManager { get; private set; }

        public uint Playing { get; private set; }
        public IPAddress PublicIP { get; private set; }
        public uint TokenCount { get { return (uint) tokens.Count; } }
        public BotState State { get; private set; }

        private DateTime nextConnect;
        private Queue<ImmutableArray<byte>> tokens;

        private SteamBot(string username, string password) {
            this.Logon = new SteamUser.LogOnDetails {
                Username = username,
                Password = password,
            };

            this.Client = new SteamClient(System.Net.Sockets.ProtocolType.Udp);
            this.Friends = Client.GetHandler<SteamFriends>();
            this.Coordinator = Client.GetHandler<SteamGameCoordinator>();
            this.User = Client.GetHandler<SteamUser>();
            this.CallbackManager = new CallbackManager(Client);

            this.State = BotState.Disconnected;
            this.nextConnect = DateTime.MaxValue;
            this.tokens = new Queue<ImmutableArray<byte>>();
        }

        public void Connect() {
            if (State != BotState.Disconnected) {
                throw new InvalidOperationException("Requested connect but not disconnected");
            }

            State = BotState.Connecting;
            Client.Connect();
        }

        public void CloseGame() {
            var msg = new ClientMsgProtobuf<CMsgClientGamesPlayed>(EMsg.ClientGamesPlayed);
            msg.Body.client_os_type = 14;
            Client.Send(msg);
        }

        public void Disconnect() {
            State = BotState.LoggingOff;
            User.LogOff();
        }

        public ImmutableArray<byte> NextToken() {
            return tokens.Dequeue();
        }

        public void Play(CMsgClientGamesPlayed.GamePlayed game) {
            var msg = new ClientMsgProtobuf<CMsgClientGamesPlayed>(EMsg.ClientGamesPlayed);
            msg.Body.games_played.Add(game);
            msg.Body.client_os_type = 14;
            Client.Send(msg);
        }

        public void SayHello(uint appId) {
            var hello = new ClientGCMsgProtobuf<CMsgClientHello>((uint) EGCBaseClientMsg.k_EMsgGCClientHello);
            Coordinator.Send(hello, appId);

            log.DebugFormat("Hello {0}", appId);
        }

        public void SetFriendsOnline(string username) {
            Friends.SetPersonaName(username);
            Friends.SetPersonaState(EPersonaState.Online);
        }

        public void Stop() {
          State = BotState.Failed;
        }

        private void Run() {
            while (State != BotState.Failed) {
                CallbackManager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }
        }

        private void OnMessage(IPacketMsg packetMsg) {
            if (packetMsg.MsgType == EMsg.ClientPlayingSessionState) {
                var pb = new ClientMsgProtobuf<CMsgClientPlayingSessionState>(packetMsg);
                Playing = pb.Body.playing_app;
            }
        }

        private void onConnected(SteamClient.ConnectedCallback callback) {
            log.DebugFormat("Connection callback from Steam, eresult: {0}", callback.Result);
            if (callback.Result != EResult.OK) {
                State = BotState.Failed;
                return;
            }

            State = BotState.LoggingIn;
            User.LogOn(Logon);
        }

        private void onDisconnected(SteamClient.DisconnectedCallback callback) {
            log.Debug("Disconnection callback from Steam");

            if (State != BotState.Failed) {
                State = BotState.Disconnected;
            }
        }

        private void onLoggedIn(SteamUser.LoggedOnCallback callback) {
            log.Debug("Logged on callback from Steam");
            if (callback.Result != EResult.OK) {
                State = BotState.Failed;

                if (callback.Result == EResult.AccountLogonDenied) {
                    log.ErrorFormat("SteamGaurd required for {0}", Logon.Username);
                } else {
                    log.ErrorFormat("Error logging in to Steam: {0}", callback.Result);
                }

                return;
            }

            PublicIP = callback.PublicIP;
            State = BotState.LoggedIn;
        }

        private void onTokensCallback(SteamApps.GameConnectTokensCallback callback) {
            foreach (byte[] token in callback.Tokens) {
                log.Debug("Got token " + BitConverter.ToString(token));
                var immutable = token.ToImmutableArray<byte>();
                if (!tokens.Contains(immutable)) {
                    tokens.Enqueue(immutable);
                }
            }

            while (callback.TokensToKeep < tokens.Count) {
                log.Debug("Dequeuing token");
                tokens.Dequeue();
            }
        }

        private void onAppOwnership(SteamApps.AppOwnershipTicketCallback pb) {
            log.Debug("Got bot ticket of " + pb.AppID + " " + pb.Result + " " + pb.Ticket.Length);
        }

        public enum BotState {
            Disconnected,
            Connecting,
            LoggingIn,
            LoggedIn,

            LoggingOff,

            Failed,
        }
    }
}
