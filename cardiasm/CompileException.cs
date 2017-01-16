using System;
using System.Runtime.Serialization;

namespace cardiasm
{
    [Serializable]
    internal class CompileException : Exception
    {
        private string operand;
        private string v;

        public CompileException()
        {
        }

        public CompileException(string message) : base(message)
        {
        }

        public CompileException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public CompileException(string v, string operand)
        {
            this.v = v;
            this.operand = operand;
        }

        protected CompileException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}