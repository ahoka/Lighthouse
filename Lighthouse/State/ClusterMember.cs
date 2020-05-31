using Grpc.Net.Client;
using Lighthouse.Protocol;
using System;
using System.Security.Cryptography.X509Certificates;
using static Lighthouse.Protocol.Raft;

namespace Lighthouse.State
{
    public class ClusterMember
    {
        public RaftClient Client { get; }

        public ClusterMember(Uri address)
        {
            var channel = GrpcChannel.ForAddress(address);
            Client = new Raft.RaftClient(channel);
        }
    }
}