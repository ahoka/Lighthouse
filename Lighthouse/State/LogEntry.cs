namespace Lighthouse.State
{
    public class LogEntry
    {
        public LogEntry(ulong term)
        {
            Term = term;
        }

        public ulong Term { get; }
    }
}