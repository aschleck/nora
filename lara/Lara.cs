using log4net;
using nora.lara.machine;
using nora.lara.net;
using nora.lara.state;
using nora.machine;
using System;
using System.Collections.Generic;
using System.Threading;

namespace nora.lara {

    public class Lara {

        private static readonly ILog log = LogManager.GetLogger(typeof(Lara));

        public static Lara Join(ConnectionDetails details, Controller controller) {
            Lara lara = new Lara();
            new Thread(() => lara.Connect(details, controller)).Start();
            return lara;
        }

        public bool Active { get; private set; }

        private Lara() {
            Active = false;
        }

        void Connect(ConnectionDetails details, Controller controller) {
            Active = true;

            var client = new Client(details.Nickname, details.SteamworksSessionId);
            var connection = Connection.CreateWith(details);
            var handshaker = new Handshaker(details, client, connection);
            var signonProcessor = new SignonProcessor(client, connection);
            var gameProcessor = new GameProcessor(client, connection);
            var userCmdGenerator = new UserCmdGenerator(client, connection);

            controller.Initialize(details.SteamId, client, connection, userCmdGenerator);

            Machines.Call tick = () => {
                controller.Tick();
                userCmdGenerator.Tick();
                client.Created.Clear();
                client.Deleted.Clear();
            };
            Machines.Call joinReset = () => {
                userCmdGenerator.Reset();
            };

            var metastates = new Dictionary<SpectateState, Metastate>() {
                { SpectateState.HANDSHAKE_REQUEST, Metastate.HANDSHAKE },
                { SpectateState.HANDSHAKE_CONNECT, Metastate.HANDSHAKE },
                { SpectateState.SIGNON_CONNECTED, Metastate.SIGNON },
                { SpectateState.SIGNON_NEW, Metastate.SIGNON },
                { SpectateState.SIGNON_PRESPAWN, Metastate.SIGNON },
                { SpectateState.SIGNON_SPAWN, Metastate.SIGNON },
                { SpectateState.SIGNON_FULL, Metastate.GAME },
            };

            var processors = new Dictionary<Metastate, Processor>() { 
                { Metastate.HANDSHAKE, handshaker },
                { Metastate.SIGNON, signonProcessor},
                { Metastate.GAME, gameProcessor }
            };

            Func<SpectateState, Machines.MachineBinding<Event, SpectateState>> Start = Machines.Start<Event, SpectateState>;
            Func<SpectateState, Machines.MachineStateBinding<Event, SpectateState>> In = Machines.In<Event, SpectateState>;
            Func<Event, Machines.MachineTransitionBinding<Event, SpectateState>> On = Machines.On<Event, SpectateState>;
            var machine = Start(SpectateState.DISCONNECTED)
                .Add(In(SpectateState.DISCONNECTED)
                    .Add(On(Event.REQUEST_CONNECT).Transit(SpectateState.HANDSHAKE_REQUEST)))
                .Add(In(SpectateState.HANDSHAKE_REQUEST)
                    .Entry(handshaker.RequestHandshake)
                    .Add(On(Event.HANDSHAKE_CHALLENGE).Transit(SpectateState.HANDSHAKE_CONNECT))
                    .Add(On(Event.REJECTED).Transit(SpectateState.REJECTED)))
                .Add(In(SpectateState.HANDSHAKE_CONNECT)
                    .Entry(handshaker.RespondHandshake)
                    .Add(On(Event.HANDSHAKE_COMPLETE).Transit(SpectateState.SIGNON_CONNECTED))
                    .Add(On(Event.REJECTED).Transit(SpectateState.REJECTED)))
                .Add(In(SpectateState.SIGNON_CONNECTED)
                    .Entry(signonProcessor.EnterConnected)
                    .Add(On(Event.GO_NEW).Transit(SpectateState.SIGNON_NEW))
                    .Add(On(Event.DISCONNECTED).Transit(SpectateState.DISCONNECTED)))
                .Add(In(SpectateState.SIGNON_NEW)
                    .Entry(signonProcessor.EnterNew)
                    .Add(On(Event.GO_CONNECTED).Transit(SpectateState.SIGNON_CONNECTED))
                    .Add(On(Event.GO_PRESPAWN).Transit(SpectateState.SIGNON_PRESPAWN))
                    .Add(On(Event.DISCONNECTED).Transit(SpectateState.DISCONNECTED)))
                .Add(In(SpectateState.SIGNON_PRESPAWN)
                    .Entry(signonProcessor.EnterPrespawn)
                    .Add(On(Event.GO_SPAWN).Transit(SpectateState.SIGNON_SPAWN))
                    .Add(On(Event.DISCONNECTED).Transit(SpectateState.DISCONNECTED)))
                .Add(In(SpectateState.SIGNON_SPAWN)
                    .Entry(signonProcessor.EnterSpawn)
                    .Add(On(Event.BASELINE).Transit(SpectateState.SIGNON_FULL))
                    .Add(On(Event.DISCONNECTED).Transit(SpectateState.DISCONNECTED)))
                .Add(In(SpectateState.SIGNON_FULL)
                    .Entry(() => { gameProcessor.EnterGame(); joinReset(); })
                    .Add(On(Event.TICK).Call(tick))
                    .Add(On(Event.DISCONNECTED).Transit(SpectateState.DISCONNECTED)))
                .Add(In(SpectateState.REJECTED))
                .Build();

            machine.Trigger(Event.REQUEST_CONNECT);

            long next_tick = DateTime.Now.Ticks;
            while (machine.State != SpectateState.DISCONNECTED &&
                   machine.State != SpectateState.REJECTED) {
                if (next_tick > DateTime.Now.Ticks) {
                    Thread.Sleep(1);
                    continue;
                }

                List<byte[]> outBand = connection.GetOutOfBand();
                List<Connection.Message> inBand = connection.GetInBand();

                foreach (byte[] message in outBand) {
                    Nullable<Event> e = processors[metastates[machine.State]].Process(message);
                    if (e.HasValue) {
                        machine.Trigger(e.Value);
                    }
                }

                foreach (Connection.Message message in inBand) {
                    Nullable<Event> e = processors[metastates[machine.State]].Process(message);
                    if (e.HasValue) {
                        machine.Trigger(e.Value);
                    }
                }

                machine.Trigger(Event.TICK);

                if (client.TickInterval > 0) {
                    next_tick += (uint) (client.TickInterval * 1000 * 10000 /* ticks per ms */);
                } else {
                    next_tick += 50 * 1000;
                }
                int remain = (int) (next_tick - DateTime.Now.Ticks) / 10000;
                if (remain > 0) {
                    Thread.Sleep(1);
                } else if (remain < 0) {
                    next_tick = DateTime.Now.Ticks;
                }
            }

            Active = false;
            if (machine.State == SpectateState.DISCONNECTED) {
            } else if (machine.State == SpectateState.REJECTED) {
            } else {
                throw new Exception("Unknown state " + machine.State);
            }
        }
    }
}
