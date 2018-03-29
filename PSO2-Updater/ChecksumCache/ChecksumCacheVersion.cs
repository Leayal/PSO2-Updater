using System;
using System.Text;

namespace Leayal.PSO2.Updater.ChecksumCache
{
    public sealed class ChecksumCacheVersion
    {
        public static readonly ChecksumCacheVersion Unknown = new ChecksumCacheVersion(new Version(0, 0, 0, 0));
        public static readonly ChecksumCacheVersion Version1 = new ChecksumCacheVersion(new Version(1, 0, 0, 0));

        private ChecksumCacheVersion(Version version)
        {
            this.Version = version;
        }
        
        public Version Version { get; }
        public static readonly byte[] Signature = Encoding.ASCII.GetBytes("DramielLeayalPSO2Checksumcache");

        public override string ToString()
        {
            return this.Version.ToString();
        }

        public override bool Equals(object obj)
        {
            return this.Version.Equals(obj);
        }

        public override int GetHashCode()
        {
            return this.Version.GetHashCode();
        }
    }
}
