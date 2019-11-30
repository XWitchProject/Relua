using System;
namespace Relua {
    /// <summary>
    /// Base class for Relua exceptions. By catching this type, you can
    /// catch both types of exceptions (while tokenizing and while parsing).
    /// </summary>
    public abstract class ReluaException : Exception {
        public int Line;
        public int Column;

        protected ReluaException(string msg) : base(msg) { }
    }

    /// <summary>
    /// Exception thrown when the tokenizer runs into invalid syntax.
    /// </summary>
    public class TokenizerException : ReluaException {
        public TokenizerException(string msg, int line, int @char)
                        : base($"Failed tokenizing: {msg} [{line}:{@char}]") {
            Line = line;
            Column = @char;
        }

        public TokenizerException(string msg, Tokenizer.Region region)
                        : base($"Failed tokenizing: {msg} [{region.BoundsToString()}]") {
            Line = region.StartLine;
            Column = region.StartColumn;
        }
    }

    /// <summary>
    /// Exception thrown when the parser runs into invalid syntax.
    /// </summary>
    public class ParserException : ReluaException {
        public ParserException(string msg, Tokenizer.Region region)
                        : base($"Failed parsing: {msg} [{region.BoundsToString()}]") {
            Line = region.StartLine;
            Column = region.StartColumn;
        }

        public ParserException(string msg) : base(msg) { }
    }
}
