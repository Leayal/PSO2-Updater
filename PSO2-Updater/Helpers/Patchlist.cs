using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Leayal.PSO2.Updater.Helpers
{
    class Patchlist<T> : IReadOnlyDictionary<string, T>
    {
        protected Dictionary<string, T> Dictionary { get; }
        public Patchlist()
        {
            this.Dictionary = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        }

        public Patchlist(IDictionary<string, T> items)
        {
            this.Dictionary = new Dictionary<string, T>(items, StringComparer.OrdinalIgnoreCase);
        }

        public void Add(string key, T value)
        {
            this.Dictionary.Add(key, value);
        }

        public override int GetHashCode()
        {
            return this.Dictionary.GetHashCode();
        }

        public void Set(string key, T value)
        {
            this.Dictionary[key] = value;
        }

        public T this[string key] => this.Dictionary[key];

        public IEnumerable<string> Keys => this.Dictionary.Keys;

        public IEnumerable<T> Values => this.Dictionary.Values;

        public int Count => this.Dictionary.Count;

        public bool ContainsKey(string key)
        {
            return this.Dictionary.ContainsKey(key);
        }

        public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
        {
            return this.Dictionary.GetEnumerator();
        }

        public bool TryGetValue(string key, out T value)
        {
            return this.Dictionary.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.Dictionary.GetEnumerator();
        }
    }
}
