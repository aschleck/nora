namespace nora.lara.machine {

    public enum Event {

        DISCONNECTED,
        REJECTED,
        REQUEST_CONNECT,
        HANDSHAKE_CHALLENGE,
        HANDSHAKE_COMPLETE,
        GO_CONNECTED,
        GO_NEW,
        GO_PRESPAWN,
        GO_SPAWN,

        BASELINE,
        TICK
    }
}
