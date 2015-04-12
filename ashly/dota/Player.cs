using nora.ashly.dsl;
using nora.lara.state;

namespace nora.ashly.dota {

    public class Player : PropertyClass {

        public TypedValue<uint> PlayerId;
        public Handle Hero;

        public Player(uint id, Client client) : base(id, client) {
            this.PlayerId = bind<uint>("DT_DOTAPlayer", "m_iPlayerID"); 
            this.Hero = handle("DT_DOTAPlayer", "m_hAssignedHero");
        }
    }
}
