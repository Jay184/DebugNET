using System;

namespace DebugNET {

    [Serializable]
    public class AttachException : Exception {
        public AttachException() { }
        public AttachException(string message) : base(message) { }
        public AttachException(string message, Exception inner) : base(message, inner) { }
        protected AttachException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
