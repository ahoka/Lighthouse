using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lighthouse.State
{
    public class Node
    {
        public Guid Id { get; }
        public Role Role { get; set; }
        public LeaderState LeaderState { get; set; }
        public PersistentState PersistentState { get; set; }
        public VolatileState VolatileState { get; set; }

        public Node(Guid id)
        {
            Id = id;
            Role = Role.Follower;
            LeaderState = null;
            PersistentState = new PersistentState(); // TODO: load from disk
            VolatileState = new VolatileState();
        }
    }
}
