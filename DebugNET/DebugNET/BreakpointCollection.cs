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
        private Debugger Debugger { get; set; }



        public BreakpointCollection(Debugger debugger) {
            Debugger = debugger;
            Dictionary = new Dictionary<IntPtr, Breakpoint>();
        }
        public BreakpointCollection(Debugger debugger, int capacity) {
            Debugger = debugger;
            Dictionary = new Dictionary<IntPtr, Breakpoint>(capacity);
        }
        public BreakpointCollection(Debugger debugger, IDictionary<IntPtr, Breakpoint> dictionary) {
            Debugger = debugger;
            Dictionary = new Dictionary<IntPtr, Breakpoint>(dictionary);
        }



        public Breakpoint this[IntPtr key] {
            get {
                return Dictionary.ContainsKey(key) ? Dictionary[key] : null;
            }
            set {
                if (Dictionary.ContainsKey(key)) Dictionary[key] = value;
                else Dictionary.Add(key, value);
            }
        }


        public void Add(IntPtr key, Breakpoint value) {
            value.Instruction = Debugger.ReadByte(key);
            Dictionary.Add(key, value);
        }
        public void Add(IntPtr key, EventHandler<BreakpointEventArgs> eventHandler, Func<BreakpointEventArgs, bool> condition = null) {
            byte instruction = Debugger.ReadByte(key);

            Breakpoint breakpoint = new Breakpoint(instruction);
            breakpoint.Hit += eventHandler;
            breakpoint.Condition = condition;
            breakpoint.Enable(Debugger, key);

            Dictionary.Add(key, breakpoint);
        }
        public void Add(KeyValuePair<IntPtr, Breakpoint> item) => Add(item.Key, item.Value);
        public void Clear() {
            foreach (var item in Dictionary) {
                item.Value.Disable(Debugger, item.Key);
            }
            Dictionary.Clear();
        }
        public bool Contains(KeyValuePair<IntPtr, Breakpoint> item) => Dictionary.Contains(item);
        public bool ContainsKey(IntPtr key) => Dictionary.ContainsKey(key);
        public bool ContainsValue(Breakpoint value) => Dictionary.ContainsValue(value);
        public bool Remove(IntPtr key) {
            if (Dictionary.ContainsKey(key)) Dictionary[key].Disable(Debugger, key);
            return Dictionary.Remove(key);
        }
        public bool Remove(KeyValuePair<IntPtr, Breakpoint> item) => Remove(item.Key);
        public bool TryGetValue(IntPtr key, out Breakpoint value) => Dictionary.TryGetValue(key, out value);
        public Breakpoint Get(IntPtr key, bool create = true) {
            if (TryGetValue(key, out Breakpoint breakpoint)) return breakpoint;
            else if (create) {
                byte instruction = Debugger.ReadByte(key);
                breakpoint = new Breakpoint(instruction);
                breakpoint.Enable(Debugger, key);
                Dictionary.Add(key, breakpoint);
            }

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
