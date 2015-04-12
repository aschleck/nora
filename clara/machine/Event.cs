using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nora.clara.machine {
    
    public enum Event {

        OPEN_REQUEST,
        ACTION_REQUEST,
        DEATH_REQUEST,

        CONNECTED_TO_STEAM,
        PLAYING_STATE_OPENED,
        PLAYING_STATE_CLOSED,
        DISCONNECTED_FROM_STEAM,

        WELCOMED_STALE_LOBBY,
        WELCOMED,
        JOINED_CHAT,
        CREATED_LOBBY,
        LEFT_LOBBY,
        PLAYER_JOINED,
        EMPTIED,

        LOBBY_READY,
        LOBBY_NOT_READY,
        GOT_APP_TICKET,
        GOT_AUTH,
        GOT_SESSION,
        GOT_TV,
        GAME_SERVER_NOT_FOUND,
        DENIED_TV,
        SERVER_RUNNING,
        SERVER_WAITING_FOR_PLAYERS,
        GAME_SERVER_QUIT,
    }
}
