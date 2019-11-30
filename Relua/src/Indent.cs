using System;
using System.IO;

namespace Relua {
    /// <summary>
    /// Representation of mutable indentation state.
    /// </summary>
    public class Indent {
        private const string NO_INDENT = "";

        public int Amount;
        public char Character;
        public int Size;

        private string _CachedString;

        public Indent(char c, int size) {
            Amount = 0;
            Character = c;
            Size = size;
        }

        public void Increase() {
            Amount += 1;
            _CachedString = null;
        }

        public void Decrease() {
            Amount -= 1;
            if (Amount < 0) Amount = 0;
            _CachedString = null;
        }

        public override string ToString() {
            if (_CachedString != null) return _CachedString;
            if (Amount == 0) return NO_INDENT;
            _CachedString = new string(Character, Amount * Size);
            return _CachedString;
        }

        public static implicit operator string(Indent i) => i.ToString();
    }

    /// <summary>
    /// `TextWriter` wrapper which automatically inserts indentation on
    /// `WriteLine` calls. If `ForceOneLine` is `true`, `WriteLine` calls will
    /// produce a single space as opposed to newline + indent.
    /// </summary>
    public class IndentAwareTextWriter {
        public Indent Indent;
        public TextWriter Writer;

        public bool ForceOneLine;

        public IndentAwareTextWriter(TextWriter writer, Indent indent = null) {
            Indent = indent ?? new Indent(' ', 4);
            Writer = writer;
        }

        public static implicit operator IndentAwareTextWriter(TextWriter w) => new IndentAwareTextWriter(w);

        public void Write(string s) => Writer.Write(s);
        public void Write(object o) => Writer.Write(o);
        public void WriteLine() {
            if (ForceOneLine) {
                Writer.Write(" ");
                return;
            }
            Writer.WriteLine();
            Writer.Write(Indent.ToString());
        }
        public void WriteLine(string s) {
            if (ForceOneLine) {
                Writer.Write(" ");
                return;
            }
            Writer.WriteLine(s);
            Writer.Write(Indent.ToString());
        }

        public void WriteLine(object o) {
            if (ForceOneLine) {
                Writer.Write(" ");
                return;
            }
            Writer.WriteLine(o);
            Writer.Write(Indent.ToString());
        }

        public void WriteIndent() {
            if (!ForceOneLine) Writer.Write(Indent);
        }

        public void IncreaseIndent() => Indent.Increase();
        public void DecreaseIndent() => Indent.Decrease();
    }
}
