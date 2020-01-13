using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Relua {
    public class Tokenizer {
        public struct Region {
            public Tokenizer Tokenizer;
            public int StartChar;
            public int StartLine;
            public int StartColumn;
            public int EndChar;
            public int EndLine;
            public int EndColumn;

            public void End() {
                EndLine = Tokenizer.CurrentLine;
                EndColumn = Tokenizer.CurrentColumn;
                EndChar = Tokenizer.CurrentIndex;
            }

            public string BoundsToString() {
                if (StartChar == EndChar) return $"{StartLine}:{StartChar}";
                return $"{StartLine}:{StartChar} -> {EndLine}:{EndChar}";
            }

            public override string ToString() {
                return Tokenizer.Data.Substring(StartChar, EndChar - StartChar);
            }
        }

        public static HashSet<char> WHITESPACE = new HashSet<char> { ' ', '\t', '\n', '\r' };
        public static HashSet<char> PUNCTUATION = new HashSet<char> {
            '#', '%', '(', ')', '*', '+', ',', '-', '.', '/', ':', ';',
            '<', '=', '>', '[', ']', '^', '{', '}', '~'
        };

        public static HashSet<string> RESERVED_KEYWORDS = new HashSet<string> {
            "and", "break", "do", "else", "elseif", "end",
            "false", "for", "function", "if", "in", "local",
            "nil", "not", "or", "repeat", "return", "then",
            "true", "until", "while"
        };

        public string Data;
        public int CurrentIndex = 0;

        public int CurrentLine = 1;
        public int CurrentColumn = 1;
        public Parser.Settings ParserSettings;

        public Tokenizer(string data, Parser.Settings settings = null) {
            ParserSettings = settings ?? new Parser.Settings();
            Data = data;
        }

        public char CurChar {
            get {
                if (CurrentIndex >= Data.Length) return '\0';

                return Data[CurrentIndex];
            }
        }

        public bool EOF => CurChar == '\0';

        public char Peek(int n = 1) {
            if (CurrentIndex + n >= Data.Length) return '\0';

            return Data[CurrentIndex + n];
        }

        public char Move(int n = 1) {
            CurrentIndex += n;
            CurrentColumn += 1;
            if (CurChar == '\n') {
                CurrentLine += 1;
                CurrentColumn = 1;
            }
            return CurChar;
        }

        public Region StartRegion() {
            return new Region {
                Tokenizer = this,
                StartLine = CurrentLine,
                StartColumn = CurrentColumn,
                StartChar = CurrentIndex
            };
        }

        private Token? _CachedPeekToken = null;
        public Token PeekToken {
            get {
                if (_CachedPeekToken.HasValue) return _CachedPeekToken.Value;
                return (_CachedPeekToken = NextToken()).Value;
            }
        }

        public void Throw(string msg) {
            throw new TokenizerException(msg, CurrentLine, CurrentColumn);
        }

        public void Throw(string msg, Region region) {
            throw new TokenizerException(msg, region);
        }

        public string ReadUntil(params char[] c) {
            var read_region = StartRegion();

            while (!EOF && !c.Contains(Move())) {}

            read_region.End();

            if (EOF) Throw($"Expected one of: {c.Inspect()}");

            return read_region.ToString();
        }

        public void SkipWhitespace() {
            while (!EOF && WHITESPACE.Contains(CurChar)) {
                Move();
            }
        }

        public string ReadQuoted(out Region reg) {
            if (CurChar != '"' && CurChar != '\'') Throw($"Expected quoted string");
            var is_single_quote = CurChar == '\'';
            Move();
            var s = new StringBuilder();
            reg = StartRegion();
            var escaped = false;
            while (true) {
                var c = CurChar;

                if (escaped) {
                    switch(c) {
                    case 'n': c = '\n'; break;
                    case 't': c = '\t'; break;
                    case 'r': c = '\r'; break;
                    case 'a': c = '\a'; break;
                    case 'b': c = '\b'; break;
                    case 'f': c = '\f'; break;
                    case 'v': c = '\v'; break;
                    case '\\': c = '\\'; break;
                    case '"': c = '"'; break;
                    case '\'': c = '\''; break;
                    default:
                        if (IsDigit(c)) {
                            var num = c - '0';
                            if (IsDigit(Peek(1))) {
                                num *= 10;
                                num += Peek(1) - '0';
                                Move();
                            }
                            if (IsDigit(Peek(1))) {
                                num *= 10;
                                num += Peek(1) - '0';
                                Move();
                            }
                            c = (char)num; break;
                        }
                        Throw($"Unknown escape sequence '\\{c}'");
                        break;
                    }

                    s.Append(c);
                    Move();
                    escaped = false;
                    continue;
                }

                if (c == '\\') {
                    escaped = true;
                    Move();
                    continue;
                } else if ((is_single_quote && c == '\'') || (!is_single_quote && c == '"')) {
                    Move();
                    break;
                }

                if (EOF) {
                    reg.End();
                    Throw($"Unterminated quoted string", reg);
                }

                s.Append(c);
                Move();
            }

            return s.ToString();
        }

        public void Expect(char[] chars) {
            if (!chars.Contains(CurChar)) Throw($"Expected one of: {chars.Inspect()}");
        }

        public string ReadIdentifier(out Region reg) {
            reg = StartRegion();
            if (CurChar != '_' && !(CurChar >= 'a' && CurChar <= 'z') && !(CurChar >= 'A' && CurChar <= 'Z')) {
                Throw($"Expected identifier start, got {CurChar.Inspect()}");
            }
            Move();
            while (!EOF && (CurChar == '_' || (CurChar >= 'a' && CurChar <= 'z') || (CurChar >= 'A' && CurChar <= 'Z') || (CurChar >= '0' && CurChar <= '9'))) {
                Move();
            }
            reg.End();
            return reg.ToString();
        }

        public string ReadNumber(out Region reg) {
            reg = StartRegion();
            if ((CurChar == '+' || CurChar == '-') && Peek(1) == '.' && !IsDigit(Peek(2))) {
                Throw($"Expected number, got {new string(new char[] { CurChar, Peek(1), Peek(2) }).Inspect()}...");
            } else if ((CurChar == '+' || CurChar == '-') && !IsDigit(Peek(1))) {
                Throw($"Expected number, got {new string(new char[] { CurChar, Peek(1) }).Inspect()}...");
            } else if (CurChar == '.' && !IsDigit(Peek(1))) {
                Throw($"Expected number, got {new string(new char[] { CurChar, Peek(1) }).Inspect()}...");
            } else if (!IsDigit(CurChar)) {
                Throw($"Expected number, got {CurChar.Inspect()}");
            }
            if (CurChar == '0' && Peek(1) == 'x') {
                Move(2);
                while (!EOF && IsHexDigit(CurChar)) {
                    Move();
                }
                reg.End();
                return reg.ToString();
            }
            Move();
            if (CurChar == '.') Move();
            while (!EOF && IsDigit(CurChar)) {
                Move();
            }

            if (CurChar == '.') {
                Move();
                while (!EOF && IsDigit(CurChar)) {
                    Move();
                }
            }

            reg.End();

            return reg.ToString();
        }

        public string ReadPunctuation(out Region reg) {
            if (!IsPunctuation(CurChar)) Throw($"Expected punctuation, got {CurChar.Inspect()}");

            var c = CurChar;
            reg = StartRegion();
            var p = Peek(1);
            var p2 = Peek(2);
            reg.End();
            Move();

            // complex punctuation
            if (c == '=' && p == '=') { reg.End(); Move(); return "=="; }
            if (c == '<' && p == '=') { reg.End(); Move(); return "<="; }
            if (c == '>' && p == '=') { reg.End(); Move(); return ">="; }
            if (c == '.' && p == '.' && p2 == '.') { reg.End(); Move(); Move(); return "..."; }
            if (c == '.' && p == '.') { reg.End(); Move(); return ".."; }
            if (c == '~' && p == '=') { reg.End(); Move(); return "~="; }

            return c.ToString();
        }

        public static bool IsIdentifierStartSymbol(char c) {
            return c == '_' || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }

        public static bool IsIdentifierContSymbol(char c) {
            return IsDigit(c) || c == '_' || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }

        public static bool IsWhitespace(char c) {
            return c == ' ' || c == '\t' || c == '\n' || c == '\r';
        }

        public static bool IsDigit(char c) {
            return c >= '0' && c <= '9';
        }

        public static bool IsHexDigit(char c) {
            return IsDigit(c) || ((c >= 'a') && (c <= 'f')) || ((c >= 'A') && (c <= 'F'));
        }

        public static bool IsPunctuation(char c) {
            return PUNCTUATION.Contains(c);
        }

        public bool SkipMultilineComment() {
            if (CurChar == '[') {
                var eq_count = 0;
                Move();
                while (CurChar == '=') {
                    eq_count += 1;
                    Move();
                }

                if (CurChar == '[') {
                    Move();
                    while (!EOF) {
                        if (CurChar == ']') {
                            Move(1);
                            var cur_eq_count = 0;
                            while (CurChar == '=') {
                                cur_eq_count += 1;
                                Move(1);
                            }

                            if (cur_eq_count == eq_count && CurChar == ']') {
                                Move();
                                break;
                            }
                        }
                        Move();
                    }
                    return true;
                }
            }

            return false;
        }

        public Token NextToken() {
            if (_CachedPeekToken.HasValue) {
                var tok = _CachedPeekToken.Value;
                _CachedPeekToken = null;
                return tok;
            }

            SkipWhitespace();

            var c = CurChar;

            if (EOF) return Token.EOF;

            while (c == '-' && Peek(1) == '-') {
                Move(2);

                if (!SkipMultilineComment()) {
                    while (!EOF && CurChar != '\n') {
                        Move(1);
                    }
                }

                if (EOF) return Token.EOF;
                SkipWhitespace();
                c = CurChar;
            }

            if (IsDigit(c)) {
                Region reg;
                var val = ReadNumber(out reg);
                return new Token(TokenType.Number, val, reg);
            } else if (IsIdentifierStartSymbol(c)) {
                Region reg;
                var val = ReadIdentifier(out reg);
                if (RESERVED_KEYWORDS.Contains(val)) {
                    return new Token(TokenType.Punctuation, val, reg);
                } else {
                    return new Token(TokenType.Identifier, val, reg);
                }
            } else if (c == '"' || c == '\'') {
                Region reg;
                var val = ReadQuoted(out reg);
                return new Token(TokenType.QuotedString, val, reg);
            } else if (IsPunctuation(c)) {
                Region reg;
                var val = ReadPunctuation(out reg);
                return new Token(TokenType.Punctuation, val, reg);
            } else {
                Throw($"Unrecognized character: {c.Inspect()}");
                throw new Exception("unreachable");
            }
        }
    }
}
