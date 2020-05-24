using System;

namespace Lighthouse.State
{
    public sealed class NodeId
    {
        public NodeId(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; }
    }
}