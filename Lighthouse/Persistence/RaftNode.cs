using System;
using System.Collections.Generic;

namespace Lighthouse.Persistence
{
    public class RaftNode
    {
        public Guid NodeId { get; set; }
        public IEnumerable<RaftPeer> Peers { get; set; }
    }
}