using nora.ashly.dsl;
using nora.lara.state;

namespace nora.ashly.dota {

    public class GameRules : PropertyClass {

        public EnumValue<GameState> GameState;
        public TypedValue<float> GameTime;
        public TypedValue<uint> HeroPickState;
        public TypedValue<float> HeroPickStateTransitionTime;
        public TypedValue<float> PreGameStartTime;

        public GameRules(uint id, Client client) : base(id, client) {
            GameState = bindE<GameState>("DT_DOTAGamerules", "m_nGameState");
            GameTime = bind<float>("DT_DOTAGamerules", "m_fGameTime");
            HeroPickState = bind<uint>("DT_DOTAGamerules", "m_nHeroPickState");
            HeroPickStateTransitionTime = bind<float>("DT_DOTAGamerules", "m_flHeroPickStateTransitionTime");
            PreGameStartTime = bind<float>("DT_DOTAGamerules", "m_flPreGameStartTime");
        }
    }

    public enum GameState {

        LOADING = 1,
        PICK_BAN = 2,
        PRE_GAME = 3,
        ACTIVE = 4,
        FINISHED = 5
    }
}
