using nora.ashly.dsl;
using nora.lara.state;

namespace nora.ashly.dota {

    public class PlayerResource : PropertyClass {

        public RangeValue<string> Names { get; private set; }
        public RangeValue<ulong> SteamIds { get; private set; }

        public PlayerResource(uint id, Client client) : base(id, client) {
            Names = range<string>("m_iszPlayerNames", 32);
            SteamIds = range<ulong>("m_iPlayerSteamIDs", 32);
        }
    }
}
