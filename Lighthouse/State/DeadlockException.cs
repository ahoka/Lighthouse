using System;
using System.Runtime.Serialization;

namespace Lighthouse.State
{
    internal class DeadlockException : Exception
    {
        public DeadlockException()
        {
        }

        public DeadlockException(string message) : base(message)
        {
        }
    }
}