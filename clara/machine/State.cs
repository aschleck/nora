using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nora.clara.machine {

    public enum State {

        CONCEIVED,
        CONNECTING_TO_STEAM,
        OPENING,

        DOTA_OPENING,
        DOTA_HOME,

        IN_STALE_LOBBY,
        IN_LOBBY,
        LOBBY_STARTING,
        JOINING_GAME,

        CONNECTING,
        GETTING_TV,
        GETTING_TICKET,
        GETTING_AUTH,
        GETTING_SESSION,
        WATCHING_GAME,
        WATCHING_GAME_NO_STEAM,
        CLOSING,
        DYING,
        DEAD
    }
}
