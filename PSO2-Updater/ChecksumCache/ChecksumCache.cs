using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using Leayal.PSO2.Updater.Events;
using System.Text;

namespace Leayal.PSO2.Updater.ChecksumCache
{
    /// <summary>
    /// 
    /// </summary>
    public class ChecksumCache : IDisposable
    {
        /// <summary>
        /// Create a new cache file with the given path. If the file is already exist, it will be overwritten.
        /// </summary>
        /// <param name="path">Path to the file.</param>
        /// <returns></returns>
        public static ChecksumCache Create(string path)
        {
            File.Create(path, 1).Close();
            return new ChecksumCache(path, false);
        }

        /// <summary>
        /// Open the cache from file and read it.
        /// </summary>
        /// <param name="path">Path to the file.</param>
        /// <returns></returns>
        public static ChecksumCache ReadFromFile(string path)
        { return new ChecksumCache(path, true); }

        /// <summary>
        /// Open the cache from file but not read it.
        /// </summary>
        /// <param name="path">Path to the file.</param>
        /// <returns></returns>
        public static ChecksumCache OpenFromFile(string path)
        { return new ChecksumCache(path, false); }

        private ConcurrentDictionary<string, PSO2FileChecksum> myCheckSumList;
        public ConcurrentDictionary<string, PSO2FileChecksum> ChecksumList
        {
            get
            {
                if (this._disposed)
                    throw new ObjectDisposedException("ChecksumCache");
                return this.myCheckSumList;
            }
        }


        private ChecksumCache(string path, bool read)
        {
            this._corruptEntryCount = 0;
            this.Filepath = path;
            if (read)
                this.ReadChecksumCache();
        }
        /// <summary>
        /// Get the cache's fullpath.
        /// </summary>
        public string Filepath { get; }
        private int _corruptEntryCount;
        /// <summary>
        /// Gets a value indicating how many invalid entries has been found and skipped. This property will always return 0 if the method <see cref="ReadChecksumCache"/> has not been called.
        /// </summary>
        public int CorruptEntryCount => this._corruptEntryCount;
        private string _Version;
        /// <summary>
        /// Gets the cache's PSO2 client version. This property will always return null if the method <see cref="ReadChecksumCache"/> has not been called.
        /// </summary>
        public string Version => this._Version;

        private bool inUsing;
        /// <summary>
        /// Gets a value indicating whether the <see cref="ChecksumCache"/> is being used by <see cref="PSO2UpdateManager"/>.
        /// </summary>
        public bool IsInUse => this.inUsing;
        internal void Lock()
        {
            this.inUsing = true;
        }
        internal void Release()
        {
            this.inUsing = false;
        }

        public void ReadChecksumCache()
        {
            if (this._disposed)
                throw new ObjectDisposedException("ChecksumCache");
            if (this.myCheckSumList != null)
                return;

            this.myCheckSumList = new ConcurrentDictionary<string, PSO2FileChecksum>(StringComparer.OrdinalIgnoreCase);

            if (File.Exists(this.Filepath))
                using (FileStream fs = File.OpenRead(this.Filepath))
                    if (fs.Length > 0)
                        try
                        {
                            using (DeflateStream gs = new DeflateStream(fs, CompressionMode.Decompress))
                            using (ChecksumCacheReader ccr = new ChecksumCacheReader(gs))
                            using (StreamReader sr = new StreamReader(gs, Encoding.UTF8))
                                if (!sr.EndOfStream)
                                {
                                    PSO2FileChecksum tmpline = null;

                                    // Read the first line, this is to get the PSO2's client version which when the file was written.
                                    // Main purpose of version is to compare if the file is valid to be used or the game client has been updated but the cache has not.
                                    this._Version = sr.ReadLine();

                                    while (!ccr.EndOfStream)
                                    {
                                        tmpline = ccr.ReadLine();
                                        if (tmpline != null)
                                            this.myCheckSumList.TryAdd(tmpline.RelativePath, tmpline);
                                        else
                                            this._corruptEntryCount++;
                                    }

                                }
                        }
                        catch (InvalidDataException dataEx)
                        {
                            this.myCheckSumList.Clear();
                            this.OnHandledException(dataEx);
                        }
        }

        /// <summary>
        /// Write the current checksum cache entries out with current PSO2 Client version.
        /// </summary>
        public void WriteChecksumCache()
        {
            this.WriteChecksumCache(this.Version);
        }

        /// <summary>
        /// Write the current checksum cache entries out with specified PSO2 Client version.
        /// </summary>
        /// <param name="pso2version">The version of PSO2 Client</param>
        public void WriteChecksumCache(string pso2version)
        {
            if (this._disposed)
                throw new ObjectDisposedException("ChecksumCache");
            if (this.myCheckSumList == null) return;

            Microsoft.VisualBasic.FileIO.FileSystem.CreateDirectory(Microsoft.VisualBasic.FileIO.FileSystem.GetParentPath(this.Filepath));

            using (FileStream fs = File.Create(this.Filepath))
            using (DeflateStream gs = new DeflateStream(fs, CompressionMode.Compress))
            using (ChecksumCacheWriter ccw = new ChecksumCacheWriter(gs))
            using (StreamWriter sr = new StreamWriter(gs, Encoding.UTF8))
            {
                sr.Write(pso2version);
                sr.Flush();

                ccw.WriteEntries(this.myCheckSumList.Values);

                ccw.Flush();
            }
            this._Version = pso2version;
        }

        public event EventHandler<HandledExceptionEventArgs> HandledException;
        protected virtual void OnHandledException(Exception ex)
        {
            this.HandledException?.Invoke(this, new HandledExceptionEventArgs(ex));
        }

        private bool _disposed;
        /// <summary>
        /// Clear the entry list. Thid method can not be called while the <see cref="ChecksumCache"/> is being used.
        /// </summary>
        public void Dispose()
        {
            if (this._disposed) return;
            if (this.IsInUse)
                throw new InvalidOperationException("The cache is currently being used. Cannot be dispose. Please call dispose after the operation is completed.");
            else
            {
                this._disposed = true;
                if (this.myCheckSumList != null)
                    this.myCheckSumList.Clear();
                this.myCheckSumList = null;
                this._Version = null;
            }
        }
    }
}
