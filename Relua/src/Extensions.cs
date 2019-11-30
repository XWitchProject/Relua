using System;
using System.Text;

namespace Relua {
    /// <summary>
    /// Collection of extension methods used internally by Relua.
    /// </summary>
    public static class Extensions {
        public static bool Contains(this Array ary, object elem) {
            for (var i = 0; i < ary.Length; i++) {
                if (ary.GetValue(i) == elem) return true;
            }
            return false;
        }

        public static bool IsASCIIPrintable(this char c) {
            return (c >= ' ' && c <= '~') || c > 128;
        }

        public static bool IsIdentifier(this string s) {
            if (s.Length == 0) return false;

            if (!Tokenizer.IsIdentifierStartSymbol(s[0])) return false;

            for (var i = 1; i < s.Length; i++) {
                var c = s[i];

                if (!Tokenizer.IsIdentifierContSymbol(c)) return false;
            }

            return true;
        }

        public static string Inspect(this Array obj) {
            var s = new StringBuilder();

            s.Append("{");
            for (var i = 0; i < obj.Length; i++) {
                s.Append(obj.GetValue(i).ToString());
                if (i < obj.Length - 1) s.Append(", ");
            }
            s.Append("}");

            return s.ToString();
        }

        public static string Inspect(this object obj) {
            if (obj is Array) return Inspect((Array)obj);
            if (obj is char) return Inspect((char)obj);
            if (obj is string) return Inspect((string)obj);
            if (obj is Tokenizer.Region) return Inspect((Tokenizer.Region)obj);
            if (obj is AST.Node) return Inspect((AST.Node)obj);
            return obj.ToString();
        }

        public static string Inspect(this AST.Node obj) {
            return $"[{obj.GetType().Name}] {obj.ToString()}";
        }

        public static string Inspect(this char obj) {
            if (obj == '\n') return "'\\n'";
            if (obj == '\t') return "'\\t'";
            if (obj == '\r') return "'\\r'";
            if (obj == '\\') return "'\\'";
            if (obj == '\0') return "'\\0'";
            return $"'{obj}'";
        }

        public static string Inspect(this Tokenizer.Region obj) {
            if (obj.StartChar == obj.EndChar) {
                return $"[{obj.StartLine},{obj.StartColumn}]";
            }
            return $"[{obj.StartLine},{obj.StartColumn}] -> [{obj.EndLine},{obj.EndColumn}]";
        }

        public static string Inspect(this string obj) {
            if (obj == null) return "null";
            return $"\"{obj.Replace("\t", "\\t").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
        }
    }
}
