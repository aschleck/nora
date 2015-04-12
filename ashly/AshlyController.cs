using log4net;
using nora.ashly.dota;
using nora.lara;
using nora.lara.net;
using nora.lara.state;
using nora.protos;
using System.Collections.Immutable;
using System.Linq;

namespace nora.ashly {

    class AshlyController : Controller {

        private static readonly ILog log = LogManager.GetLogger(typeof(AshlyController));

        private ulong steamId;
        private Client client;
        private Connection connection;
        private Commander commander;
        private bool reserved;
        private bool commanded;

        private Zoo zoo;
        private uint? ourPlayerId;
        private uint? ourPlayer;
        private uint? lastTick;

        public void Initialize(ulong id, Client client, Connection connection, Commander commander) {
            this.steamId = id;
            this.client = client;
            this.connection = connection;
            this.commander = commander;
            this.zoo = null;
        }

        public void Tick() {
            UpdateEntities();

            if (!zoo.Has<GameRules>() || !zoo.Has<PlayerResource>()) {
                return;
            }

            var resource = zoo.GetSingle<PlayerResource>();
            if (!ourPlayerId.HasValue) {
                for (uint i = 0; i < resource.SteamIds.Value.Length; ++i) {
                    if (steamId == resource.SteamIds.Value[i]) {
                        ourPlayerId = i;
                        break;
                    }
                }
            }

            if (ourPlayerId.HasValue && !ourPlayer.HasValue) {
                ourPlayer = zoo
                    .Get<Player>()
                    .Where((p) => p.PlayerId.Value == ourPlayerId)
                    .SingleOrDefault()
                    .Id;
            }

            if (!ourPlayerId.HasValue) {
                return;
            }

            var us = zoo.Get<Player>(ourPlayer.Value);
            var rules = zoo.GetSingle<GameRules>();

            if (rules.GameState.Value == GameState.PICK_BAN 
                    && rules.HeroPickState.Value == 1
                    && !reserved) {
                foreach (string command in new string[] {
                            "dota_select_hero npc_dota_hero_furion reserve",
                            "dota_select_hero npc_dota_hero_npc_dota_hero_furion"
                        }) {
                    var cmd = new CNETMsg_StringCmd();
                    cmd.command = command;
                    var message = Connection.ConvertProtoToMessage<CNETMsg_StringCmd>(
                        (uint) NET_Messages.net_StringCmd, cmd);
                    connection.SendReliably(message);
                }
                reserved = true;
                log.Info("Sent Nature's Prophet reserve");
            } else if (rules.GameState.Value == GameState.ACTIVE
                    && us.Hero.Value.HasValue
                    && !commanded) {
                commander.Submit(Order.MakeMouseClick(
                    ImmutableList.Create(us.Hero.Value.Value),
                    new Vector(-6219.188f, -5654.906f, 261.0313f)));
                commanded = true;
                log.Info("Made move command");
            } else if (rules.GameState.Value == GameState.ACTIVE
                    && us.Hero.Value.HasValue) {
                        if (!lastTick.HasValue || lastTick != resource.TreeStateRadiant.ReadAt) {
                    log.Warn(string.Join(
                        ",", 
                        resource.TreeStateRadiant.Value.Select((x) => x.ToString())));
                    lastTick = resource.TreeStateRadiant.ReadAt.Value;
                }
            }
        }

        private void UpdateEntities() {
            if (zoo != null) {
                zoo.Tick();   
            } else if (client.Classes.Count > 0 && zoo == null) {
                this.zoo = new Zoo.Builder(client)
                    .Associate<GameRules>(
                        client.ClassesByName["CDOTAGamerulesProxy"],
                        (i, c) => new GameRules(i, c))
                    .Associate<Player>(
                        client.ClassesByName["CDOTAPlayer"],
                        (i, c) => new Player(i, c))
                    .Associate<PlayerResource>(
                        client.ClassesByName["CDOTA_PlayerResource"],
                        (i, c) => new PlayerResource(i, c))
                    .Build();
                zoo.Tick();
            }
        }
    }
}
