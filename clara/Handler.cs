using SteamKit2;

namespace nora.clara {

    class Handler<T> : ClientMsgHandler {

        private OnMessage del;

        public Handler(OnMessage del) {
            this.del = del;
        }

        public override void HandleMsg(IPacketMsg packetMsg) {
            del(packetMsg);
        }

        public delegate void OnMessage(IPacketMsg packetMsg);
    }
}
