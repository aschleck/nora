using log4net;
using System.Collections.Generic;

namespace nora.machine {

    public class MachineImpl<E, S> : Machine<E, S> where S : struct {

        private static ILog log = LogManager.GetLogger(typeof(MachineImpl<E, S>));

        public S State { get; private set; }
        private Dictionary<S, Effect> effects;

        internal MachineImpl(S state, Dictionary<S, Effect> effects) {
            this.State = state;
            this.effects = effects;
        }

        public void Trigger(E e) {
            log.Debug("Triggered " + e + " in state " + State);
            Effect current = effects[State];

            if (!current.Transitions.ContainsKey(e)) {
                return;
            }

            Transition transition = current.Transitions[e];
            Effect next = effects[transition.Next.HasValue ? transition.Next.Value : State];
            bool change = transition.Next != null && !State.Equals(transition.Next);

            if (change && current.Exit != null) {
                current.Exit();
            }

            if (transition.Call != null) {
                transition.Call();
            }

            if (change) {
                log.Debug("Transitioning to " + transition.Next);
                State = transition.Next.Value;
            }

            if (change && next.Enter != null) {
                next.Enter();
            }
        }

        internal class Effect {

            public Machines.Call Enter { get; set; }
            public Machines.Call Exit { get; set; }
            public Dictionary<E, Transition> Transitions { get; set; }
        }

        internal class Transition {

            public Machines.Call Call { get; set; }
            public S? Next { get; set; }

            public Transition() {
                Call = null;
                Next = null;
            }
        }
    }
}
