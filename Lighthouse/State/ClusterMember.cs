using Grpc.Core;
using Lighthouse.Protocol;
using System;
using System.Security.Cryptography.X509Certificates;
using static Lighthouse.Protocol.Raft;

namespace Lighthouse.State
{
    public class ClusterMember
    {
        public Guid NodeId { get; }
        public Uri Address { get; }

        public RaftClient Client { get; }

        public ClusterMember(Guid id, Uri address)
        {
            NodeId = id;
            Address = address;

            var channel = new Channel(address.ToString(), ChannelCredentials.Insecure);
            Client = new Raft.RaftClient(channel);
        }
    }
}