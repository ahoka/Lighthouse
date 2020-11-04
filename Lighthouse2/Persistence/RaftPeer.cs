using System;

namespace Lighthouse.Persistence
{
    public class RaftPeer
    {
        public Guid NodeId { get; set; }
        public Uri Address { get; set; }
    }
}