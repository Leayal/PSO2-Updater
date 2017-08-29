namespace Leayal.PSO2.Updater.ChecksumCache
{
    public struct ChecksumCacheEntry
    {
        public string Name { get; }
        public ulong Size { get; }
        public string Hash { get; }

        public override bool Equals(object obj)
        {
            if (obj is ChecksumCacheEntry)
            {
                ChecksumCacheEntry target = (ChecksumCacheEntry)obj;
                if (this.Name == target.Name)
                    if (this.Hash == target.Hash)
                        if (this.Size == target.Size)
                            return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode() ^ this.Size.GetHashCode() ^ this.Hash.GetHashCode();
        }

        public override string ToString()
        {
            // Microsoft.VisualBasic.ControlChars.Tab or '\t' doesn't matter
            return string.Concat(this.Name, Microsoft.VisualBasic.ControlChars.Tab, this.Size.ToString(), Microsoft.VisualBasic.ControlChars.Tab, this.Hash);
        }

        internal ChecksumCacheEntry(string _name, ulong _size, string _hash)
        {
            this.Name = _name;
            this.Size = _size;
            this.Hash = _hash;
        }

        public static readonly ChecksumCacheEntry Empty = new ChecksumCacheEntry(string.Empty, 0, string.Empty);

        public static bool operator ==(ChecksumCacheEntry ccr1, ChecksumCacheEntry ccr2)
        {
            return ccr1.Equals(ccr2);
        }
        public static bool operator !=(ChecksumCacheEntry ccr1, ChecksumCacheEntry ccr2)
        {
            return !(ccr1 == ccr2);
        }
    }
}
