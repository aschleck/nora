using nora.lara.machine;
using nora.lara.net;

namespace nora.lara {

    public interface Processor {

        Event? Process(byte[] message);
        Event? Process(Connection.Message message);
    }
}
