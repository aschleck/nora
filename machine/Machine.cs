using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nora.machine {

    public interface Machine<E, S> where S : struct {

        S State { get; }

        void Trigger(E e);
    }
}
