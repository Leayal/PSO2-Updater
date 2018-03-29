using System;
using System.Collections;
using System.Collections.Generic;
using Leayal.Security.Cryptography;
using System.Text;
using System.IO;
using Microsoft.VisualBasic;
using Leayal.PSO2.Updater.Helpers;

namespace Leayal.PSO2.Updater
{
    /// <summary>
    /// Determine the type of the patch list
    /// </summary>
    [Flags]
    public enum PatchListType : byte
    {
        /// <summary>
        /// List that hold all the required files for the game.
        /// </summary>
        Master = 1 << 0,
        /// <summary>
        /// List that hold the "new files" over the master list.
        /// </summary>
        Patch = 1 << 1,
        /// <summary>
        /// List that hold the launcher files. This list use old format
        /// </summary>
        LauncherList = 1 << 2
    }

    /*
    public class PatchItem
    {
        internal static PatchItem FromFile(string rootdirectory, string relativepath)
        {
            string fullpath = Path.Combine(rootdirectory, relativepath);
            if (File.Exists(fullpath))
                using (FileStream fs = File.OpenRead(fullpath))
                    return new PatchItem(relativepath, fs.Length, MD5Wrapper.HashFromStream(fs));
            else
                return new PatchItem(relativepath, 0, string.Empty);
        }

        public string RelativePath { get; }
        public long Size { get; }
        public string MD5Hash { get; }
        public string Source { get; }

        internal PatchItem(string relativePath, long filesize, string hash)
        {
            this.RelativePath = relativePath;
            this.Size = filesize;
            this.MD5Hash = hash;
        }

        public override bool Equals(object obj)
        {
            if (obj is PatchItem item)
                return string.Equals(item.MD5Hash, this.MD5Hash, StringComparison.OrdinalIgnoreCase);
            else if (obj is PatchItem pso2item)
                return string.Equals(pso2item.MD5Hash, this.MD5Hash, StringComparison.OrdinalIgnoreCase);
            else
                return false;
        }

        public override int GetHashCode()
        {
            return this.MD5Hash.GetHashCode();
        }

        public override string ToString()
        {
            return this.RelativePath + ControlChars.Tab + this.Size.ToString() + ControlChars.Tab + this.MD5Hash;
        }
    }
    */
    public class RemotePatchlist : IReadOnlyDictionary<string, PSO2File>
    {
        private Patchlist<PSO2File> list;

        public IEnumerable<string> Keys => this.list.Keys;

        public IEnumerable<PSO2File> Values => this.list.Values;

        public int Count => this.list.Count;

        public PSO2File this[string key] => this.list[key];

        /// <summary>
        /// Add missing <seealso cref="PSO2File"/> from the destination list and update existing <seealso cref="PSO2File"/> in the current list
        /// </summary>
        /// <param name="list">The destination list</param>
        public void Merge(RemotePatchlist list) => this.Merge(list, true);

        /// <summary>
        /// Add missing <seealso cref="PSO2File"/> from the destination list and update existing <seealso cref="PSO2File"/> in the current list
        /// </summary>
        /// <param name="list">The destination list</param>
        /// <param name="ignoreExisting">Determine if the existing <seealso cref="PSO2File"/> in the current list will also be updated</param>
        public void Merge(RemotePatchlist list, bool ignoreExisting)
        {
            foreach (var value in list.Values)
            {
                if (ignoreExisting)
                    this.TryAdd(value);
                else
                    this.AddOrUpdate(value);
            }
        }

        internal RemotePatchlist()
        {
            this.list = new Patchlist<PSO2File>();
        }

        internal RemotePatchlist(IDictionary<string, PSO2File> items)
        {
            this.list = new Patchlist<PSO2File>(items);
        }

        internal void Add(PSO2File value)
        {
            this.list.Add(value.Filename, value);
        }

        internal bool TryAdd(PSO2File value)
        {
            if (this.ContainsKey(value.Filename))
                return false;
            this.Add(value);
            return true;
        }

        internal void AddOrUpdate(PSO2File value)
        {
            if (this.ContainsKey(value.Filename))
                this.list.Set(value.Filename, value);
            else
                this.Add(value);
        }

        public bool ContainsKey(string key)
        {
            return this.list.ContainsKey(key);
        }

        public bool TryGetValue(string key, out PSO2File value)
        {
            return this.list.TryGetValue(key, out value);
        }

        public IEnumerator<KeyValuePair<string, PSO2File>> GetEnumerator()
        {
            return this.list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.list.GetEnumerator();
        }
    }
}
