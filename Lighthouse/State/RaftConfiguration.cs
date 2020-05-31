using System;
using System.Collections.Generic;

namespace Lighthouse.State
{
    public class RaftConfiguration
    {
        public Uri Address { get; set; }
        public IEnumerable<RaftPeer> Peers { get; set; }
    }
}