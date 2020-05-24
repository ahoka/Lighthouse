using Google.Protobuf.WellKnownTypes;

namespace Lighthouse.State
{
    public sealed class Term
    {
        public Term(ulong value)
        {
            Value = value;
        }

        public ulong Value { get; }
    }
}