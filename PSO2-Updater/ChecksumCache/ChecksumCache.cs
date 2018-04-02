using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using Leayal.PSO2.Updater.Events;
using System.Text;
using System.Collections.Generic;
using SharpCompress.Compressors.Deflate;
using Leayal.PSO2.Updater.Helpers;

namespace Leayal.PSO2.Updater.ChecksumCache
{
    /// <summary>
    /// 
    /// </summary>
    public class ChecksumCache : IDisposable
    {
        /// <summary>
        /// Validate if the file is a valid <see cref="ChecksumCache"/> file.
        /// </summary>
        /// <param name="filepath">Path to the ChecksumCache file.</param>
        /// <returns>Boolean</returns>
        public static bool IsValid(string filepath)
        {
            using (FileStream fs = File.OpenRead(filepath))
                return IsValid(fs, false);
        }

        /// <summary>
        /// Validate if the stream contains a valid <see cref="ChecksumCache"/> object.
        /// </summary>
        /// <param name="source">Source stream</param>
        /// <returns>Boolean</returns>
        public static bool IsValid(Stream source)
        {
            return IsValid(source, true);
        }

        /// <summary>
        /// Validate if the stream contains a valid <see cref="ChecksumCache"/> object.
        /// </summary>
        /// <param name="source">Source stream</param>
        /// <param name="preserveStreamPosition">Determine if the stream's position should be preserve after check.</param>
        /// <returns>Boolean</returns>
        public static bool IsValid(Stream source, bool preserveStreamPosition)
        {
            if (preserveStreamPosition && !source.CanSeek)
                throw new InvalidOperationException("The stream should be seekable.");

            // Take longest length
            int lengthToRead = ChecksumCacheVersion.Signature.Length;
            byte[] bytes = new byte[lengthToRead];
            int readbyte = source.Read(bytes, 0, bytes.Length);
            if (readbyte > 0)
            {
                if (preserveStreamPosition)
                    source.Seek(readbyte * -1, SeekOrigin.Begin);
                
                // Begin check
                if (bytes.IndexOf(ChecksumCacheVersion.Signature) == 0)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Create a new cache file with the given path. If the file is already exist, it will be overwritten.
        /// </summary>
        /// <param name="path">Path to the file.</param>
        /// <returns></returns>
        public static ChecksumCache Create(string path)
        {
            FileStream fs = File.Create(path);
            return new ChecksumCache(fs, false);
        }

        /// <summary>
        /// Open the cache from file but not read it.
        /// </summary>
        /// <param name="path">Path to the file.</param>
        /// <returns></returns>
        public static ChecksumCache OpenFromFile(string path)
        {
            FileStream fs = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            if (!IsValid(fs, false))
            {
                fs.Dispose();
                throw new InvalidCacheException("The file is not a valid cache storage.");
            }

            return new ChecksumCache(fs, true);
        }

        private ConcurrentDictionary<string, PSO2FileChecksum> myCheckSumList;
        public ConcurrentDictionary<string, PSO2FileChecksum> ChecksumList
        {
            get
            {
                return this.myCheckSumList;
            }
        }

        private FileStream fs;

        private ChecksumCache() : base()
        {
            this._corruptEntryCount = 0;
        }
        private ChecksumCache(string path, bool read) : this()
        {
            this.Filepath = path;
            if (read)
                this.ReadChecksumCache();
        }
        private ChecksumCache(FileStream fileStream, bool read) : this()
        {
            this.Filepath = fileStream.Name;
            this.fs = fileStream;
            if (read)
                this.ReadChecksumCache(this.fs);
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
        private string _PSO2Version;
        /// <summary>
        /// Gets the cache's PSO2 client version. This property will always return null if the method <see cref="ReadChecksumCache"/> has not been called.
        /// </summary>
        public string PSO2Version => this._PSO2Version;

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
        private Version _checksumVersion;
        public Version ChecksumVersion => this._checksumVersion;

        public void ReadChecksumCache()
        {
            if (this.fs == null)
                this.fs = File.Open(this.Filepath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            this.ReadChecksumCache(this.fs);
        }

        public void ReadChecksumCache(FileStream stream)
        {
            if (this._disposed)
                throw new ObjectDisposedException("ChecksumCache");
            if (this.myCheckSumList != null)
                return;

            if (stream.Length > (ChecksumCacheVersion.Signature.Length + 4))
                try
                {
                    if (stream.Position != ChecksumCacheVersion.Signature.Length)
                        stream.Seek(ChecksumCacheVersion.Signature.Length, SeekOrigin.Begin);

                    using (BinaryReader br = new BinaryReader(stream, Encoding.Unicode, true))
                        this._checksumVersion = new Version(br.ReadByte(), br.ReadByte(), br.ReadByte(), br.ReadByte());

                    using (ZlibStream compressStream = new ZlibStream(stream, CompressionMode.Decompress, CompressionLevel.BestCompression, true, Encoding.Unicode))
                    using (BinaryReader br = new BinaryReader(compressStream, Encoding.Unicode, true))
                    using (ChecksumCacheReader ccr = new ChecksumCacheReader(compressStream))
                    {
                        this._PSO2Version = br.ReadString();
                        int count = br.ReadInt32();
                        Dictionary<string, PSO2FileChecksum> dict = new Dictionary<string, PSO2FileChecksum>(count, StringComparer.OrdinalIgnoreCase);
                        PSO2FileChecksum tmpline = null;
                        for (int i = 0; i < count; i++)
                        {
                            tmpline = ccr.ReadLine();
                            if (tmpline != null)
                                dict[tmpline.RelativePath] = tmpline;
                            else
                                this._corruptEntryCount++;
                        }
                        this.myCheckSumList = new ConcurrentDictionary<string, PSO2FileChecksum>(dict, StringComparer.OrdinalIgnoreCase);
                    }
                }
                catch (Exception)
                {
                    if (this.myCheckSumList == null)
                        this.myCheckSumList = new ConcurrentDictionary<string, PSO2FileChecksum>(StringComparer.OrdinalIgnoreCase);
                    else
                        this.myCheckSumList.Clear();
                }
            else
            {
                if (this.myCheckSumList == null)
                    this.myCheckSumList = new ConcurrentDictionary<string, PSO2FileChecksum>(StringComparer.OrdinalIgnoreCase);
                else
                    this.myCheckSumList.Clear();
            }
        }

        /// <summary>
        /// Write the current checksum cache entries out with current PSO2 Client version.
        /// </summary>
        public void WriteChecksumCache()
        {
            this.WriteChecksumCache(this.PSO2Version);
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

            if (this.fs == null)
                this.fs = File.Open(this.Filepath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);

            if (this.fs.Position != 0)
                this.fs.Seek(0, SeekOrigin.Begin);

            this.fs.Write(ChecksumCacheVersion.Signature, 0, ChecksumCacheVersion.Signature.Length);
            Version ver = ChecksumCacheVersion.Version1.Version;
            this.fs.WriteByte((byte)ver.Major);
            this.fs.WriteByte((byte)ver.Minor);
            this.fs.WriteByte((byte)ver.Build);
            this.fs.WriteByte((byte)ver.Revision);

            using (ZlibStream compressStream = new ZlibStream(this.fs, CompressionMode.Compress, CompressionLevel.BestCompression, true, Encoding.Unicode))
            using (BinaryWriter bw = new BinaryWriter(compressStream, Encoding.Unicode, true))
            using (ChecksumCacheWriter ccw = new ChecksumCacheWriter(compressStream))
            {
                bw.Write(pso2version);
                bw.Write(this.myCheckSumList.Count);

                ccw.WriteEntries(this.myCheckSumList.Values);
            }
            this.fs.Flush();
            long currentpost = this.fs.Position;
            this.fs.SetLength(currentpost);

            this._PSO2Version = pso2version;
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
                this._PSO2Version = null;
                if (this.fs != null)
                    this.fs.Dispose();
            }
        }
    }
}
