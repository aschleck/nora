using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nora.machine {

    public class Machines {

        public delegate void Call();

        public static MachineBinding<E, S> Start<E, S>(S initial) where S : struct {
            return new MachineBinding<E, S>(initial);
        }

        public static MachineStateBinding<E, S> In<E, S>(S state) where S : struct {
            return new MachineStateBinding<E, S>(state);
        }

        public static MachineTransitionBinding<E, S> On<E, S>(E e) where S : struct {
            return new MachineTransitionBinding<E, S>(e);
        }

        public class MachineBinding<E, S> where S : struct {

            private S initial;
            private List<MachineStateBinding<E, S>> states;

            public MachineBinding(S initial) {
                this.initial = initial;
                this.states = new List<MachineStateBinding<E, S>>();
            }

            public MachineBinding<E, S> Add(MachineStateBinding<E, S> binding) {
                this.states.Add(binding);
                return this;
            }

            public Machine<E, S> Build() {
                var effects = new Dictionary<S, MachineImpl<E, S>.Effect>();
                foreach (var binding in states) {
                    var effect = new MachineImpl<E, S>.Effect();
                    effects[binding.state] = effect;
                    effect.Enter = binding.enter;
                    effect.Exit = binding.exit;
                    effect.Transitions = new Dictionary<E, MachineImpl<E, S>.Transition>();

                    foreach (var transBinding in binding.transitions) {
                        var transition = new MachineImpl<E, S>.Transition();
                        effect.Transitions[transBinding.e] = transition;
                        transition.Call = transBinding.call;
                        transition.Next = transBinding.state;
                    }
                }

                return new MachineImpl<E, S>(initial, effects);
            }
        }

        public class MachineStateBinding<E, S> where S : struct {

            public S state;
            public Machines.Call enter;
            public Machines.Call exit;
            public List<MachineTransitionBinding<E, S>> transitions;

            public MachineStateBinding(S state) {
                this.state = state;
                this.enter = null;
                this.exit = null;
                this.transitions = new List<MachineTransitionBinding<E, S>>();
            }

            public MachineStateBinding<E, S> Entry(Machines.Call enter) {
                this.enter = enter;
                return this;
            }

            public MachineStateBinding<E, S> Exit(Machines.Call exit) {
                this.exit = exit;
                return this;
            }

            public MachineStateBinding<E, S> Add(MachineTransitionBinding<E, S> transition) {
                this.transitions.Add(transition);
                return this;
            }
        }

        public class MachineTransitionBinding<E, S> where S : struct {

            public E e;
            public Machines.Call call;
            public S? state;

            public MachineTransitionBinding(E e) {
                this.e = e;
                this.call = null;
                this.state = null;
            }

            public MachineTransitionBinding<E, S> Call(Machines.Call call) {
                this.call = call;
                return this;
            }

            public MachineTransitionBinding<E, S> Transit(S state) {
                this.state = state;
                return this;
            }
        }
    }
}
