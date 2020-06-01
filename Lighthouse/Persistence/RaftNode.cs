using System;
using System.Collections.Generic;

namespace Lighthouse.Persistence
{
    public class RaftNode
    {
        public Guid NodeId { get; }
        public IEnumerable<RaftPeer> Peers { get; set; }
    }
}