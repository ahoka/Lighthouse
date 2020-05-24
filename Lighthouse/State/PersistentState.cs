using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lighthouse.State
{
    public sealed class PersistentState
    {
        public PersistentState()
        {
            CurrentTerm = new Term(0);
            VotedFor = null;
            Log = new List<LogEntry>();
        }

        // latest term server has seen (initialized to 0 on first boot, increases monotonically)
        public Term CurrentTerm { get; set; } 
        
        // candidate Id that received vote in currentterm(or null if none)
        public NodeId VotedFor { get; set; }
        
        // log entries; each entry contains command for state machine, and term when entry was received by leader (first index is 1)
        public IList<LogEntry> Log { get; set; }
    }
}
