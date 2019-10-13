using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DebugNET {
    public class BreakpointCollection : IEnumerable<KeyValuePair<IntPtr, Breakpoint2>> {
        public ICollection<IntPtr> Keys => Dictionary.Keys;
        public ICollection<Breakpoint2> Values => Dictionary.Values;
        public int Count => Dictionary.Count;

        private Dictionary<IntPtr, Breakpoint2> Dictionary { get; set; }
        private Debugger2 Debugger { get; set; }



        public BreakpointCollection(Debugger2 debugger) {
            Debugger = debugger;
            Dictionary = new Dictionary<IntPtr, Breakpoint2>();
        }
        public BreakpointCollection(Debugger2 debugger, int capacity) {
            Debugger = debugger;
            Dictionary = new Dictionary<IntPtr, Breakpoint2>(capacity);
        }
        public BreakpointCollection(Debugger2 debugger, IDictionary<IntPtr, Breakpoint2> dictionary) {
            Debugger = debugger;
            Dictionary = new Dictionary<IntPtr, Breakpoint2>(dictionary);
        }



        public Breakpoint2 this[IntPtr key] {
            get {
                return Dictionary.ContainsKey(key) ? Dictionary[key] : null;
            }
            set {
                if (Dictionary.ContainsKey(key)) Dictionary[key] = value;
                else Dictionary.Add(key, value);
            }
        }


        public void Add(IntPtr key, Breakpoint2 value) {
            value.Instruction = Debugger.ReadByte(key);
            Dictionary.Add(key, value);
        }
        public void Add(KeyValuePair<IntPtr, Breakpoint2> item) => Add(item.Key, item.Value);
        public void Clear() {
            foreach (var item in Dictionary) {
                item.Value.Disable(Debugger, item.Key);
            }
            Dictionary.Clear();
        }
        public bool Contains(KeyValuePair<IntPtr, Breakpoint2> item) => Dictionary.Contains(item);
        public bool ContainsKey(IntPtr key) => Dictionary.ContainsKey(key);
        public bool ContainsValue(Breakpoint2 value) => Dictionary.ContainsValue(value);
        public bool Remove(IntPtr key) {
            if (Dictionary.ContainsKey(key)) Dictionary[key].Disable(Debugger, key);
            return Dictionary.Remove(key);
        }
        public bool Remove(KeyValuePair<IntPtr, Breakpoint2> item) => Remove(item.Key);
        public bool TryGetValue(IntPtr key, out Breakpoint2 value) => Dictionary.TryGetValue(key, out value);
        public Breakpoint2 Get(IntPtr key, bool create = true) {
            if (TryGetValue(key, out Breakpoint2 breakpoint)) return breakpoint;
            else if (create) {
                byte instruction = Debugger.ReadByte(key);
                breakpoint = new Breakpoint2(instruction);
                breakpoint.Enable(Debugger, key);
                Dictionary.Add(key, breakpoint);
            }

            return breakpoint;
        }


        public IEnumerator<KeyValuePair<IntPtr, Breakpoint2>> GetEnumerator() {
            foreach (var item in Dictionary) {
                yield return item;
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
