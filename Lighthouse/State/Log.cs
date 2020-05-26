using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Lighthouse.State
{
    public class Log
    {
        private List<LogEntry> Entries { get; }
        private ReaderWriterLockSlim Lock { get; }

        public ulong LastIndex => (ulong)(Entries.Count - 1);

        public Log()
        {
            Entries = new List<LogEntry>();
            Lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        }

        public LogEntry Get(ulong index)
        {
            try
            {
                Lock.EnterReadLock();
                return Entries.FirstOrDefault(x => x.Index == index);
            }
            finally
            {
                Lock.ExitReadLock();
            }
        }

        public void Purge(ulong inclusiveStartIndex)
        {
            try
            {
                Lock.EnterWriteLock();
                Entries.RemoveRange((int)inclusiveStartIndex, Entries.Count - 1);
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        public bool Append(LogEntry entry)
        {
            try
            {
                Lock.EnterWriteLock();

                if (entry.Index != (ulong)Entries.Count)
                {
                    return false;
                }

                Entries.Add(entry);
            }
            finally
            {
                Lock.ExitWriteLock();
            }

            return true;
        }
    }
}
