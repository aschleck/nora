using nora.lara.state;

namespace nora.lara {

    public interface Commander {

        void Submit(Order order);
    }
}
