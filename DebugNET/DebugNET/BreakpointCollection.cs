using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DebugNET {
    public class BreakpointCollection : IEnumerable<KeyValuePair<IntPtr, Breakpoint>> {
        public ICollection<IntPtr> Keys => Dictionary.Keys;
        public ICollection<Breakpoint> Values => Dictionary.Values;
        public int Count => Dictionary.Count;

        private Dictionary<IntPtr, Breakpoint> Dictionary { get; set; }



        public BreakpointCollection() {
            Dictionary = new Dictionary<IntPtr, Breakpoint>();
        }
        public BreakpointCollection(int capacity) {
            Dictionary = new Dictionary<IntPtr, Breakpoint>(capacity);
        }
        public BreakpointCollection(IDictionary<IntPtr, Breakpoint> dictionary) {
            Dictionary = new Dictionary<IntPtr, Breakpoint>(dictionary);
        }



        public Breakpoint this[IntPtr key] {
            get {
                return Dictionary.ContainsKey(key) ? Dictionary[key] : null;
            } set {
                if (Dictionary.ContainsKey(key)) Dictionary[key] = value;
                else Dictionary.Add(key, value);
            }
        }


        public void Add(IntPtr key, Breakpoint value) => Dictionary.Add(key, value);
        public void Add(KeyValuePair<IntPtr, Breakpoint> item) => Dictionary.Add(item.Key, item.Value);
        public void Clear() => Dictionary.Clear();
        public bool Contains(KeyValuePair<IntPtr, Breakpoint> item) => Dictionary.Contains(item);
        public bool ContainsKey(IntPtr key) => Dictionary.ContainsKey(key);
        public bool ContainsValue(Breakpoint value) => Dictionary.ContainsValue(value);
        public bool Remove(IntPtr key) => Dictionary.Remove(key);
        public bool Remove(KeyValuePair<IntPtr, Breakpoint> item) => Dictionary.Remove(item.Key);
        public bool TryGetValue(IntPtr key, out Breakpoint value) => Dictionary.TryGetValue(key, out value);
        public Breakpoint Get(IntPtr key) {
            if (TryGetValue(key, out Breakpoint breakpoint)) return breakpoint;
           
            breakpoint = new Breakpoint();
            Add(key, breakpoint);
            return breakpoint;
        }

        public IEnumerator<KeyValuePair<IntPtr, Breakpoint>> GetEnumerator() {
            foreach (var item in Dictionary) {
                yield return item;
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
