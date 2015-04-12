using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nora.machine {

    public class BufferedMachine<E, S> : Machine<E, S> where S : struct {

        private Machine<E, S> root;
        private ConcurrentQueue<E> events;

        public bool Available { get { return !events.IsEmpty; } }
        public S State { get { return root.State; } }

        public BufferedMachine(Machine<E, S> root) {
            this.root = root;
            this.events = new ConcurrentQueue<E>();
        }

        public void Consume() {
            E e;
            while (events.TryDequeue(out e)) {
                root.Trigger(e);
            }
        }

        public void Trigger(E e) {
            events.Enqueue(e);
        }
    }
}
