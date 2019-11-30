using System;
namespace Relua {
    public enum TokenType {
        EOF,
        Identifier,
        QuotedString,
        Number,
        Punctuation
    }

    public struct Token {
        public static readonly Token EOF = new Token(TokenType.EOF, null, new Tokenizer.Region());

        public TokenType Type;
        public string Value;
        public Tokenizer.Region Region;

        public Token(TokenType type, string value, Tokenizer.Region reg) {
            Type = type;
            Value = value;
            Region = reg;
        }

        public override string ToString() {
            return $"TOKEN {{type = {Type}, value = {Value.Inspect()}, region = {Region.Inspect()}}}";
        }

        public bool Is(TokenType type, string value) {
            return Type == type && Value == value;
        }

        public bool IsEOF() {
            return Type == TokenType.EOF;
        }

        public bool IsIdentifier(string value) {
            return Is(TokenType.Identifier, value);
        }

        public bool IsQuotedString(string value) {
            return Is(TokenType.QuotedString, value);
        }

        public bool IsNumber(string value) {
            return Is(TokenType.Number, value);
        }

        public bool IsPunctuation(string value) {
            return Is(TokenType.Punctuation, value);
        }
    }
}
