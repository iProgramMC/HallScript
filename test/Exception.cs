using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HallScript
{

    [Serializable]
    public class ParseFailException : Exception
    {
        public ParseFailException() { }
        public ParseFailException(int line, string message) : base((line != -1 ? "" : (" in line " + line.ToString() + ": ")) + message) { }
        public ParseFailException(int line, string message, Exception inner) : base((line != -1 ? "" : (" in line " + line.ToString() + ": ")) + message, inner) { }
        protected ParseFailException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
