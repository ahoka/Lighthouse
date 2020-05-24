using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lighthouse.State
{
    public sealed class LeaderState
    {
        // for each server, index of the next log entry to send to that server (initialized to leader last log index + 1)
        public IDictionary<NodeId, LogIndex> NextIndex { get; }

        // for each server, index of highest log entryknown to be replicated on server(initialized to 0, increases monotonically)
        public IDictionary<NodeId, LogIndex> MatchIndex { get; }

        public LeaderState(IEnumerable<NodeId> members, LogIndex lastLogIndex)
        {
            NextIndex = members.ToDictionary(m => m, _ => new LogIndex(lastLogIndex.Value + 1));
            MatchIndex = members.ToDictionary(m => m, _ => new LogIndex(0));
        }
    }
}
