using log4net;
using nora.clara;
using nora.clara.machine;
using nora.lara;
using nora.machine;
using System;
using System.Net;
using System.Threading;

namespace nora.ashly {

    public class Ashly {

        private static readonly ILog log = LogManager.GetLogger(typeof(Ashly));

        public static void Main(string[] args) {
            log4net.Config.XmlConfigurator.Configure();

            new Ashly().Execute();
        }

        private void Execute() {
            var bot = SteamBot.Create("username", "password");
            var client = new Client(bot);
            var machine = MakeMachine(bot, client);
            var coordinator = new Coordinator<State>(machine, client, bot);

            machine.Trigger(Event.OPEN_REQUEST);

            while (machine.State != State.DEAD &&
                   bot.State != SteamBot.BotState.Failed) {
                while (machine.Available) {
                    machine.Consume();
                }
                Thread.Sleep(100);
            }
        }

        private static BufferedMachine<Event, State> MakeMachine(
                SteamBot bot, Client client) {
            Machines.Call connect = () => {
                bot.Connect();
            };
            Machines.Call open = () => {
                bot.SetFriendsOnline(bot.Logon.Username);
                client.Launch(Games.DotaGameId);
            };
            Machines.Call sayHello = () => {
                client.SayHello();
            };
            Machines.Call leaveLobby = () => {
                if (client.Lobby.connect == null) {
                    client.LeaveLobby();
                } else {
                    client.AbandonCurrentGame();
                }
            };
            Machines.Call getTicket = () => {
                client.GetAppTicket();
            };
            Machines.Call createLobby = () => {
                client.CreateLobby("cow");
            };
            Machines.Call joinRadiant = () => {
                client.JoinLobbySlot(
                    SteamKit2.GC.Dota.Internal.DOTA_GC_TEAM.DOTA_GC_TEAM_GOOD_GUYS);
            };
            Machines.Call startLobby = () => {
                client.LaunchLobby();
            };
            Machines.Call getAuth = () => {
                client.CreateAuthTicket();
                client.Auth();
            };
            Machines.Call beginSession = () => {
                client.BeginGameServerSession();
            };
            Machines.Call join = () => {
                string[] split = client.Lobby.connect.Split(':');
                var endpoint = new IPEndPoint(IPAddress.Parse(split[0]), Int16.Parse(split[1]));

                Lara.Join(new ConnectionDetails() {
                    Endpoint = endpoint,
                    SteamId = bot.Client.SteamID.ConvertToUInt64(),
                    Nickname = bot.Logon.Username,
                    SteamworksSessionId = client.SteamworksSessionId,
                    Ticket = client.TicketForServer,
                    Secret = client.Lobby.pass_key
                }, new AshlyController());
            };

            Func<State, Machines.MachineBinding<Event, State>> Start =
                Machines.Start<Event, State>;
            Func<State, Machines.MachineStateBinding<Event, State>> In =
                Machines.In<Event, State>;
            Func<Event, Machines.MachineTransitionBinding<Event, State>> On =
                Machines.On<Event, State>;
            var unbuffered = Start(State.CONCEIVED)
                .Add(In(State.CONCEIVED)
                    .Add(On(Event.OPEN_REQUEST).Transit(State.CONNECTING_TO_STEAM)))
                .Add(In(State.CONNECTING_TO_STEAM)
                    .Entry(connect)
                    .Add(On(Event.CONNECTED_TO_STEAM).Transit(State.OPENING))
                    .Add(On(Event.DISCONNECTED_FROM_STEAM).Call(connect).Transit(State.CONNECTING_TO_STEAM)))
                .Add(In(State.OPENING)
                    .Entry(open)
                    .Add(On(Event.PLAYING_STATE_OPENED).Transit(State.CONNECTING)))
                .Add(In(State.CONNECTING)
                    .Entry(sayHello)
                    .Add(On(Event.WELCOMED).Transit(State.GETTING_TICKET))
                    .Add(On(Event.WELCOMED_STALE_LOBBY).Transit(State.IN_STALE_LOBBY)))
                .Add(In(State.IN_STALE_LOBBY)
                    .Entry(leaveLobby)
                    .Add(On(Event.LEFT_LOBBY).Transit(State.GETTING_TICKET)))
                .Add(In(State.GETTING_TICKET)
                    .Entry(getTicket)
                    .Add(On(Event.GOT_APP_TICKET).Transit(State.DOTA_HOME)))
                .Add(In(State.DOTA_HOME)
                    .Entry(createLobby)
                    .Add(On(Event.CREATED_LOBBY).Transit(State.IN_LOBBY)))
                .Add(In(State.IN_LOBBY)
                    .Entry(joinRadiant)
                    .Add(On(Event.LOBBY_READY).Transit(State.LOBBY_STARTING)))
                .Add(In(State.LOBBY_STARTING)
                    .Entry(startLobby)
                    .Add(On(Event.SERVER_RUNNING).Transit(State.GETTING_AUTH)))
                .Add(In(State.GETTING_AUTH)
                    .Entry(getAuth)
                    .Add(On(Event.GOT_AUTH).Transit(State.GETTING_SESSION)))
                .Add(In(State.GETTING_SESSION)
                    .Entry(beginSession)
                    .Add(On(Event.GOT_SESSION).Transit(State.JOINING_GAME)))
                .Add(In(State.JOINING_GAME)
                    .Entry(join))
                .Build();
            return new BufferedMachine<Event, State>(unbuffered);
        }
    }
}
