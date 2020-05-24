using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lighthouse.State
{
    public sealed class LogIndex
    {
        public LogIndex(ulong value)
        {
            Value = value;
        }

        public ulong Value { get; }

        public static implicit operator LogIndex(ulong value) => new LogIndex(value);
    }
}
