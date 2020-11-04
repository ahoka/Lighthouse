using System;
using System.Runtime.Serialization;

namespace Lighthouse.State
{
    [Serializable]
    internal class NodeNotInitializedException : Exception
    {
        public NodeNotInitializedException()
        {
        }

        public NodeNotInitializedException(string message) : base(message)
        {
        }

        public NodeNotInitializedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected NodeNotInitializedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}