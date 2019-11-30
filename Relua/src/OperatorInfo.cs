using System;
using System.Collections.Generic;
using Relua.AST;

namespace Relua {
    /// <summary>
    /// Struct representing a Lua operator (unary and/or binary), along with
    /// its precedence and associativity.
    /// </summary>
    public struct OperatorInfo {
        /// <summary>
        /// Dictionary of builtin Lua operators.
        /// </summary>
        public static readonly Dictionary<string, OperatorInfo> LuaOperators = new Dictionary<string, OperatorInfo> {
            ["or"] = new OperatorInfo(true, false, "or", 2, false),
            ["and"] = new OperatorInfo(true, false, "and", 3, false),
            ["<"] = new OperatorInfo(true, false, "<", 4, false),
            ["<="] = new OperatorInfo(true, false, "<=", 4, false),
            [">"] = new OperatorInfo(true, false, ">", 4, false),
            [">="] = new OperatorInfo(true, false, ">=", 4, false),
            ["~="] = new OperatorInfo(true, false, "~=", 4, false),
            ["=="] = new OperatorInfo(true, false, "==", 4, false),
            [".."] = new OperatorInfo(true, false, "..", 5, true),
            ["+"] = new OperatorInfo(true, false, "+", 6, false),
            ["-"] = new OperatorInfo(true, true, "-", 6, false),
            ["*"] = new OperatorInfo(true, false, "*", 7, false),
            ["/"] = new OperatorInfo(true, false, "/", 7, false),
            ["%"] = new OperatorInfo(true, false, "%", 7, false),
            ["not"] = new OperatorInfo(false, true, "not", 8, false),
            ["#"] = new OperatorInfo(false, true, "#", 8, false),
            ["^"] = new OperatorInfo(true, false, "^", 9, true),
        };

        public static bool BinaryOperatorExists(string key) {
            return LuaOperators.ContainsKey(key) && LuaOperators[key].IsBinary;
        }

        public static OperatorInfo? FromToken(Token tok) {
            if (tok.IsEOF()) return null;
            if (LuaOperators.TryGetValue(tok.Value, out OperatorInfo op)) return op;
            return null;
        }

        public enum OperatorType {
            Binary,
            Unary
        }

        public string TokenValue;
        public int Precedence;
        public bool RightAssociative;
        public bool IsBinary;
        public bool IsUnary;

        public OperatorInfo(bool is_binary, bool is_unary, string value, int precedence, bool right_assoc) {
            IsBinary = is_binary;
            IsUnary = is_unary;
            TokenValue = value;
            Precedence = precedence;
            RightAssociative = right_assoc;
        }

        public AST.BinaryOp.OpType? BinaryOpType {
            get {
                switch (TokenValue) {
                case "or": return BinaryOp.OpType.Or;
                case "and": return BinaryOp.OpType.And;
                case "<": return BinaryOp.OpType.LessThan;
                case ">": return BinaryOp.OpType.GreaterThan;
                case "<=": return BinaryOp.OpType.LessOrEqual;
                case ">=": return BinaryOp.OpType.GreaterOrEqual;
                case "~=": return BinaryOp.OpType.NotEqual;
                case "==": return BinaryOp.OpType.Equal;
                case "..": return BinaryOp.OpType.Concat;
                case "+": return BinaryOp.OpType.Add;
                case "-": return BinaryOp.OpType.Subtract;
                case "*": return BinaryOp.OpType.Multiply;
                case "/": return BinaryOp.OpType.Divide;
                case "%": return BinaryOp.OpType.Modulo;
                case "^": return BinaryOp.OpType.Power;
                default: return null;
                }
            }
        }

        public AST.UnaryOp.OpType? UnaryOpType {
            get {
                switch (TokenValue) {
                case "-": return UnaryOp.OpType.Negate;
                case "not": return UnaryOp.OpType.Invert;
                case "#": return UnaryOp.OpType.Length;
                default: return null;
                }
            }
        }
    }
}
