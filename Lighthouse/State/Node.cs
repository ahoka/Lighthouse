using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

        private SemaphoreSlim Semaphore => new SemaphoreSlim(1, 1);
        
        public IDisposable Lock()
        {
            return new Guard(Semaphore);
        }

        private class Guard : IDisposable
        {
            private SemaphoreSlim sem;

            public Guard(SemaphoreSlim sem)
            {
                this.sem = sem;
                var entered = sem.Wait(TimeSpan.FromSeconds(30));
                if (!entered)
                {
                    throw new DeadlockException();
                }
            }

            public void Dispose()
            {
                sem.Release();
            }
        }

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
