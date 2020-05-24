namespace Lighthouse.State
{
    public class LogEntry
    {
        public LogEntry(Term term)
        {
            Term = term;
        }

        public Term Term { get; }
    }
}