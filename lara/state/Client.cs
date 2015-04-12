using nora.protos;
using System.Collections.Generic;
using System.Linq;

namespace nora.lara.state {

    public class Client {

        public string Name { get; private set; }
        public ulong SteamworksSessionId { get; private set; }
        public int Baseline { get; set; }
        public uint ServerCount { get; set; }
        public float TickInterval { get; set; }
        public uint ClientTick { get; set; }
        public uint ServerTick { get; set; }
        public Dictionary<string, string> CVars { get; private set; }
        public List<StringTable> Strings { get; private set; }
        public Dictionary<string, int> StringsIndex { get; private set; }

        public List<EntityClass> Classes { get; private set; }
        public Dictionary<string, EntityClass> ClassesByName { get; private set; }
        public List<SendTable> SendTables { get; private set; }
        public List<FlatTable> FlatTables { get; private set; }
        public Dictionary<PropertyHandle, Property> Properties { get; private set; }
        public Dictionary<uint, Slot> Slots { get; private set; }

        public List<uint> Created { get; private set; }
        public List<uint> Deleted { get; private set; }

        public Client(string name, ulong steamworksSessionId) {
            this.Name = name;
            this.SteamworksSessionId = steamworksSessionId;
            this.ClientTick = 0;
            this.ServerTick = 0;
            this.CVars = new Dictionary<string, string>();
            this.Strings = new List<StringTable>();
            this.StringsIndex = new Dictionary<string, int>();
            this.Classes = new List<EntityClass>();
            this.ClassesByName = new Dictionary<string, EntityClass>();
            this.SendTables = new List<SendTable>();
            this.FlatTables = new List<FlatTable>();
            this.Properties = new Dictionary<PropertyHandle, Property>();
            this.Slots = new Dictionary<uint, Slot>();

            this.Created = new List<uint>();
            this.Deleted = new List<uint>();

            Reset();
        }

        public void Reset() {
            CVars.Clear();
            CVars.Add("tv_nochat", "0");
            CVars.Add("joy_autoaimdampen", "0");
            CVars.Add("name", Name);
            CVars.Add("cl_interp_ratio", "2");
            CVars.Add("tv_listen_voice_indices", "0");
            CVars.Add("cl_predict", "0");
            CVars.Add("cl_updaterate", "30");
            CVars.Add("cl_showhelp", "1");
            CVars.Add("steamworks_sessionid_lifetime_client", "0");
            CVars.Add("cl_mouselook", "1");
            CVars.Add("steamworks_sessionid_client", "0");
            CVars.Add("dota_mute_cobroadcasters", "0");
            CVars.Add("voice_loopback", "0");
            CVars.Add("dota_player_initial_skill", "0");
            CVars.Add("cl_lagcompensation", "1");
            CVars.Add("closecaption", "0");
            CVars.Add("cl_language", "english");
            CVars.Add("english", "1");
            CVars.Add("cl_class", "default");
            CVars.Add("snd_voipvolume", "1");
            CVars.Add("snd_musicvolume", "1");
            CVars.Add("cl_cmdrate", "30");
            CVars.Add("net_maxroutable", "1200");
            CVars.Add("cl_team", "default");
            CVars.Add("rate", "80000");
            CVars.Add("cl_predictweapons", "1");
            CVars.Add("cl_interpolate", "1");
            CVars.Add("cl_interp", "0.05");
            CVars.Add("dota_camera_edgemove", "1");
            CVars.Add("snd_gamevolume", "1");
            CVars.Add("cl_spec_mode", "1");

            Classes.Clear();
            ClassesByName.Clear();
            SendTables.Clear();
            FlatTables.Clear();
            Properties.Clear();
            Slots.Clear();
            Strings.Clear();
            StringsIndex.Clear();

            Created.Clear();
            Deleted.Clear();
        }

        public CMsg_CVars ExposeCVars() {
            CMsg_CVars exposed = new CMsg_CVars();

            exposed.cvars.AddRange(CVars.Select(kv => {
                var var = new CMsg_CVars.CVar();
                var.name = kv.Key;
                var.value = kv.Value;
                return var;
            }));

            return exposed;
        }

        public struct PropertyHandle {

            public uint Entity { get; set; }
            public string Table { get; set; }
            public string Name { get; set; }

            public override bool Equals(object o) {
                if (!(o is PropertyHandle)) {
                    return false;
                }
                
                PropertyHandle handle = (PropertyHandle) o;
                return Entity == handle.Entity &&
                    Table.Equals(handle.Table) &&
                    Name.Equals(handle.Name);
            }

            public override int GetHashCode() {
                int result = (int)Entity;
                result = 31 * result + Table.GetHashCode();
                result = 31 * result + Name.GetHashCode();
                return result;
            }
        }

        public class Slot {

            public Entity Entity { get; set; }
            public bool Live { get; set; }
            public Entity[] Baselines { get; set; }

            public Slot(Entity entity) {
                this.Entity = entity;
                this.Live = true;
                this.Baselines = new Entity[2];
            }
        }
    }
}
