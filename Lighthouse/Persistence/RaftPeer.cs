using System;

namespace Lighthouse.Persistence
{
    public class RaftPeer
    {
        public Guid NodeId { get; set; }
        public string Address { get; set; }
    }
}