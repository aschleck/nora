using nora.lara.net;
using nora.lara.state;

namespace nora.lara {

    public interface Controller {

        void Initialize(ulong id, Client client, Connection connection, Commander commander);
        void Tick();
    }
}
