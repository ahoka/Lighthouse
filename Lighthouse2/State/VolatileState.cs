using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lighthouse.State
{
    public class VolatileState
    {
        // index of highest log entry known to be committed (initialized to 0, increases monotonically)
        public ulong CommitIndex { get; set; }

        // index of highest log entry applied to statemachine (initialized to 0, increases monotonically)
        public ulong LastApplied { get; set; }

        public VolatileState()
        {
            CommitIndex = 0;
            LastApplied = 0;
        }
    }
}
