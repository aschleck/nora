using nora.ashly.dsl;
using nora.lara.state;

namespace nora.ashly.dota {

    public class Ability : PropertyClass {

        public TypedValue<uint> Level;

        public Ability(uint id, Client client) : base(id, client) {
            this.Level = bind<uint>("DT_DOTABaseAbility", "m_iLevel"); 
        }
    }
}
