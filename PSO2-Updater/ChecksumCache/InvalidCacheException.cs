using System;

namespace Leayal.PSO2.Updater.ChecksumCache
{
    public class InvalidCacheException : Exception
    {
        public InvalidCacheException(string message) : base(message) { }

        public InvalidCacheException(string message, Exception innerEx) : base(message, innerEx) { }

        public InvalidCacheException() : base() { }
    }
}
