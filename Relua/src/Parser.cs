using System;
using System.Collections.Generic;
using System.IO;
using Relua.AST;

namespace Relua {
    public class Parser {
        /// <summary>
        /// Settings which control certain behavior of the parser.
        /// </summary>
        public class Settings {
            /// <summary>
            /// Automatically creates NumberLiterals for sequential elements in
            /// a table constructor (ones that do not have a key specified).
            /// 
            /// Note that if this option is `false`, `AST.TableConstructor.Entry`'s
            /// `Key` field may be `null`. That field will never be `null` if this
            /// option is set to `true`.
            /// </summary>
            public bool AutofillSequentialKeysInTableConstructor = true;

            /// <summary>
            /// Automatically creates NilLiterals for all values of empty local
            /// assignments (in the style of `local a`).
            /// 
            /// Note that if this option is `false`, `AST.Assignment`'s `Values`
            /// list will be empty for local declarations. If it is set to the
            /// default `true`, the `Values` list will always match the `Targets`
            /// list in size in that case with all entries being `NilLiteral`s.
            /// </summary>
            public bool AutofillValuesInLocalDeclaration = true;

            /// <summary>
            /// Automatically fills in the `Step` field of `AST.NumericFor` with
            /// a `NumberLiteral` of value `1` if the statement did not specify
            /// the step expression.
            /// </summary>
            public bool AutofillNumericForStep = true;

            /// <summary>
            /// If `true`, will parse LuaJIT long numbers (in the form `0LL`)
            /// into the special AST node `AST.LuaJITLongLiteral`.
            /// </summary>
            public bool EnableLuaJITLongs = true;

            /// <summary>
            /// There are certain syntax quirks such as accessing the fields of
            /// a string literal (e.g. "abc":match(...)) which Lua will throw a
            /// syntax error upon seeing, but the Relua parser will happily accept
            /// (and correctly write). If this option is enabled, all Lua behavior
            /// is imitated, including errors where they are not strictly necessary.
            /// </summary>
            public bool MaintainSyntaxErrorCompatibility = false;
        }

        public Tokenizer Tokenizer;
        public Settings ParserSettings;

        public Token CurToken;

        public void Move() {
            if (CurToken.Type == TokenType.EOF) return;
            CurToken = Tokenizer.NextToken();
        }

        public Token PeekToken => Tokenizer.PeekToken;

        public void Throw(string msg, Token tok) {
            throw new ParserException(msg, tok.Region);
        }

        public void ThrowExpect(string expected, Token tok) {
            throw new ParserException($"Expected {expected}, got {tok.Type} ({tok.Value.Inspect()})", tok.Region);
        }

        public Parser(string data, Settings settings = null) : this(new Tokenizer(data, settings), settings) { }

        public Parser(StreamReader r, Settings settings = null) : this(new Tokenizer(r.ReadToEnd(), settings), settings) { }

        public Parser(Tokenizer tokenizer, Settings settings = null) {
            ParserSettings = settings ?? new Settings();
            Tokenizer = tokenizer;
            CurToken = tokenizer.NextToken();
        }

        public NilLiteral ReadNilLiteral() {
            if (CurToken.Value != "nil") ThrowExpect("nil", CurToken);
            Move();
            return NilLiteral.Instance;
        }

        public VarargsLiteral ReadVarargsLiteral() {
            if (CurToken.Value != "...") ThrowExpect("varargs literal", CurToken);
            Move();
            return VarargsLiteral.Instance;
        }

        public BoolLiteral ReadBoolLiteral() {
            var value = false;
            if (CurToken.Value == "true") value = true;
            else if (CurToken.Value == "false") value = false;
            else ThrowExpect("bool literal", CurToken);
            Move();
            return value ? BoolLiteral.TrueInstance : BoolLiteral.FalseInstance;
        }

        public Variable ReadVariable() {
            if (CurToken.Type != TokenType.Identifier) ThrowExpect("identifier", CurToken);
            if (Tokenizer.RESERVED_KEYWORDS.Contains(CurToken.Value)) Throw($"Cannot use reserved keyword '{CurToken.Value}' as variable name", CurToken);

            var name = CurToken.Value;

            Move();
            return new Variable { Name = name };
        }

        public StringLiteral ReadStringLiteral() {
            if (CurToken.Type != TokenType.QuotedString) ThrowExpect("quoted string", CurToken);
            var value = CurToken.Value;
            Move();
            return new StringLiteral { Value = value };
        }

        public NumberLiteral ReadNumberLiteral() {
            if (CurToken.Type != TokenType.Number) ThrowExpect("number", CurToken);

            if (CurToken.Value.StartsWith("0x", StringComparison.InvariantCulture)) {
                if (!int.TryParse(CurToken.Value.Substring(2), System.Globalization.NumberStyles.AllowHexSpecifier, null, out int hexvalue)) {
                    ThrowExpect("hex number", CurToken);
                }

                Move();

                return new NumberLiteral { Value = hexvalue, HexFormat = true };
            }

            if (!double.TryParse(CurToken.Value, out double value)) {
                ThrowExpect("number", CurToken);
            }

            Move();
            return new NumberLiteral { Value = value };
        }

        public LuaJITLongLiteral ReadLuaJITLongLiteral() {
            if (CurToken.Type != TokenType.Number) ThrowExpect("long number", CurToken);
            if (CurToken.Value.StartsWith("0x", StringComparison.InvariantCulture)) {
                if (!long.TryParse(CurToken.Value, System.Globalization.NumberStyles.HexNumber | System.Globalization.NumberStyles.AllowHexSpecifier, null, out long hexvalue)) {
                    ThrowExpect("hex number", CurToken);
                }

                Move();

                return new LuaJITLongLiteral { Value = hexvalue, HexFormat = true };
            }

            if (!long.TryParse(CurToken.Value, out long value)) {
                ThrowExpect("number", CurToken);
            }

            Move();
            if (!CurToken.IsIdentifier("LL")) ThrowExpect("'LL' suffix", CurToken);
            Move();
            return new LuaJITLongLiteral { Value = value };
        }

        public TableAccess ReadTableAccess(IExpression table_expr, bool allow_colon = false) {
            TableAccess table_node = null;

            if (CurToken.IsPunctuation(".") || (allow_colon && CurToken.IsPunctuation(":"))) {
                Move();
                if (CurToken.Type != TokenType.Identifier) ThrowExpect("identifier", CurToken);
                var index = new StringLiteral { Value = CurToken.Value };
                Move();
                table_node = new TableAccess { Table = table_expr, Index = index };
            } else if (CurToken.IsPunctuation("[")) {
                Move();
                var index = ReadExpression();
                if (!CurToken.IsPunctuation("]")) ThrowExpect("closing bracket", CurToken);
                Move();
                table_node = new TableAccess { Table = table_expr, Index = index };
            } else ThrowExpect("table access", CurToken);

            return table_node;
        }

        public FunctionCall ReadFunctionCall(IExpression func_expr, IExpression self_expr = null) {
            if (!CurToken.IsPunctuation("(")) ThrowExpect("start of argument list", CurToken);
            Move();

            var args = new List<IExpression>();

            if (self_expr != null) {
                args.Add(self_expr);
            }

            if (!CurToken.IsPunctuation(")")) args.Add(ReadExpression());

            while (CurToken.IsPunctuation(",")) {
                Move();
                var expr = ReadExpression();
                args.Add(expr);
                if (!CurToken.IsPunctuation(",") && !CurToken.IsPunctuation(")")) ThrowExpect("comma or end of argument list", CurToken);
            }
            if (!CurToken.IsPunctuation(")")) ThrowExpect("end of argument list", CurToken);
            Move();

            return new FunctionCall { Function = func_expr, Arguments = args };
        }


        public TableConstructor.Entry ReadTableConstructorEntry() {
            if (CurToken.Type == TokenType.Identifier) {
                var eq = PeekToken;
                if (eq.IsPunctuation("=")) {
                    // { a = ... }

                    var key = new StringLiteral { Value = CurToken.Value };
                    Move();
                    Move(); // =
                    var value = ReadExpression();
                    return new TableConstructor.Entry { ExplicitKey = true, Key = key, Value = value };
                } else {
                    // { a }
                    var value = ReadExpression();
                    return new TableConstructor.Entry { ExplicitKey = false, Value = value };
                    // Note - Key is null
                    // This is filled in in ReadTableConstructor
                }
            } else if (CurToken.IsPunctuation("[")) {
                // { [expr] = ... }
                Move();
                var key = ReadExpression();
                if (!CurToken.IsPunctuation("]")) ThrowExpect("end of key", CurToken);
                Move();
                if (!CurToken.IsPunctuation("=")) ThrowExpect("assignment", CurToken);
                Move();
                var value = ReadExpression();
                return new TableConstructor.Entry { ExplicitKey = true, Key = key, Value = value };
            } else {
                // { expr }
                return new TableConstructor.Entry { ExplicitKey = false, Value = ReadExpression() };
                // Note - Key is null
                // This is filled in in ReadTableConstructor
            }
        }

        public TableConstructor ReadTableConstructor() {
            if (!CurToken.IsPunctuation("{")) ThrowExpect("table constructor", CurToken);
            Move();

            var entries = new List<TableConstructor.Entry>();

            var cur_sequential_idx = 1;

            if (!CurToken.IsPunctuation("}")) {
                var ent = ReadTableConstructorEntry();
                if (ParserSettings.AutofillSequentialKeysInTableConstructor && ent.Key == null) {
                    ent.Key = new NumberLiteral { Value = cur_sequential_idx };
                    cur_sequential_idx += 1;
                }
                entries.Add(ent);
            }

            while (CurToken.IsPunctuation(",")) {
                Move();
                if (CurToken.IsPunctuation("}")) break; // trailing comma
                var ent = ReadTableConstructorEntry();
                if (ParserSettings.AutofillSequentialKeysInTableConstructor && ent.Key == null) {
                    ent.Key = new NumberLiteral { Value = cur_sequential_idx };
                    cur_sequential_idx += 1;
                }
                entries.Add(ent);
                if (!CurToken.IsPunctuation(",") && !CurToken.IsPunctuation("}")) ThrowExpect("comma or end of entry list", CurToken);
            }

            if (!CurToken.IsPunctuation("}")) ThrowExpect("end of entry list", CurToken);
            Move();

            return new TableConstructor { Entries = entries };
        }

        public FunctionDefinition ReadFunctionDefinition(bool start_from_params = false, bool self = false) {
            if (!start_from_params) {
                if (!CurToken.IsPunctuation("function")) ThrowExpect("function", CurToken);
                Move();
            }

            if (!CurToken.IsPunctuation("(")) ThrowExpect("start of argument name list", CurToken);
            Move();

            var varargs = false;
            var args = new List<string>();

            if (self) args.Add("self");

            if (!CurToken.IsPunctuation(")")) {
                if (CurToken.Type != TokenType.Identifier) ThrowExpect("identifier", CurToken);
                args.Add(CurToken.Value);
                Move();
            }

            while (CurToken.IsPunctuation(",")) {
                Move();
                if (CurToken.IsPunctuation("...")) {
                    varargs = true;
                    Move();
                    break;
                }
                if (CurToken.Type != TokenType.Identifier) ThrowExpect("identifier", CurToken);
                args.Add(CurToken.Value);
                Move();
            }

            if (!CurToken.IsPunctuation(")")) ThrowExpect("end of argument name list", CurToken);
            Move();

            SkipSemicolons();

            var statements = new List<IStatement>();
            while (!CurToken.IsPunctuation("end") && !CurToken.IsEOF()) {
                statements.Add(ReadStatement());
            }

            Move();

            return new FunctionDefinition {
                ArgumentNames = args,
                Block = new Block { Statements = statements },
                AcceptsVarargs = varargs,
                ImplicitSelf = self
            };
        }

        // Primary expression:
        // - Does not depend on any expressions.
        public IExpression ReadPrimaryExpression() {
            if (CurToken.Type == TokenType.QuotedString) {
                return ReadStringLiteral();
            }

            if (CurToken.Type == TokenType.Number) {
                if (ParserSettings.EnableLuaJITLongs && PeekToken.IsIdentifier("LL")) {
                    return ReadLuaJITLongLiteral();
                } else {
                    return ReadNumberLiteral();
                }
            }

            if (CurToken.Type == TokenType.Punctuation) {
                if (CurToken.Value == "{") return ReadTableConstructor();
                if (CurToken.Value == "...") return ReadVarargsLiteral();
                if (CurToken.Value == "nil") return ReadNilLiteral();
                if (CurToken.Value == "true" || CurToken.Value == "false") {
                    return ReadBoolLiteral();
                }
                if (CurToken.Value == "function") {
                    return ReadFunctionDefinition();
                }

            } else if (CurToken.Type == TokenType.Identifier) {
                return ReadVariable();
            }

            ThrowExpect("expression", CurToken);
            throw new Exception("unreachable");
        }

        public OperatorInfo? GetBinaryOperator(Token tok) {
            if (tok.Value == null) return null;
            var op = OperatorInfo.FromToken(tok);
            if (op == null) return null;
            if (!op.Value.IsBinary) ThrowExpect("binary operator", tok);

            return op.Value;
        }

        // Secondary expression:
        // - Depends on (alters the value of) *one* expression.
        public IExpression ReadSecondaryExpression() {
            var unary_op = OperatorInfo.FromToken(CurToken);

            if (unary_op != null && unary_op.Value.IsUnary) {
                Move();
            }

            IExpression expr;

            if (CurToken.IsPunctuation("(")) {
                Move();
                var complex = ReadComplexExpression(ReadSecondaryExpression(), 0, true);
                if (!CurToken.IsPunctuation(")")) {
                    ThrowExpect("closing parenthesis", CurToken);
                }
                Move();
                expr = complex;
                if (expr is FunctionCall) {
                    ((FunctionCall)expr).ForceTruncateReturnValues = true;
                }
            } else expr = ReadPrimaryExpression();

            while (CurToken.IsPunctuation(".") || CurToken.IsPunctuation("[")) {
                if (expr is FunctionCall) ((FunctionCall)expr).ForceTruncateReturnValues = false;

                if (expr is StringLiteral && ParserSettings.MaintainSyntaxErrorCompatibility) {
                    Throw($"syntax error compat: can't directly index strings, use parentheses", CurToken);
                }
                expr = ReadTableAccess(expr);
            }

            while (CurToken.IsPunctuation(":")) {
                if (expr is FunctionCall) ((FunctionCall)expr).ForceTruncateReturnValues = false;

                if (expr is StringLiteral && ParserSettings.MaintainSyntaxErrorCompatibility) {
                   Throw($"syntax error compat: can't directly index strings, use parentheses", CurToken);
                }
                var self_expr = expr;
                expr = ReadTableAccess(expr, allow_colon: true);
                expr = ReadFunctionCall(expr, self_expr);
            }

            if (CurToken.IsPunctuation("(")) {
                if (expr is FunctionCall) ((FunctionCall)expr).ForceTruncateReturnValues = false;

                if (expr is StringLiteral && ParserSettings.MaintainSyntaxErrorCompatibility) {
                    Throw($"syntax error compat: can't directly call strings, use parentheses", CurToken); 
                }
                expr = ReadFunctionCall(expr);
            } else if (CurToken.IsPunctuation("{")) {
                if (expr is FunctionCall) ((FunctionCall)expr).ForceTruncateReturnValues = false;

                if (expr is StringLiteral && ParserSettings.MaintainSyntaxErrorCompatibility) {
                    Throw($"syntax error compat: can't directly call strings, use parentheses", CurToken);
                }
                expr = new FunctionCall {
                    Function = expr,
                    Arguments = new List<IExpression> { ReadTableConstructor() }
                };
            } else if (CurToken.Type == TokenType.QuotedString) {
                if (expr is FunctionCall) ((FunctionCall)expr).ForceTruncateReturnValues = false;

                if (expr is StringLiteral && ParserSettings.MaintainSyntaxErrorCompatibility) {
                    Throw($"syntax error compat: can't directly call strings, use parentheses", CurToken);
                }
                expr = new FunctionCall {
                    Function = expr,
                    Arguments = new List<IExpression> { ReadStringLiteral() }
                };
            }

            if (unary_op != null && unary_op.Value.IsUnary) {
                if (expr is FunctionCall) ((FunctionCall)expr).ForceTruncateReturnValues = false;

                expr = new UnaryOp(unary_op.Value.UnaryOpType.Value, expr);
            }

            return expr;
        }

        // Complex expression:
        // - Depends on (alters the value of) *two* expressions.
        public IExpression ReadComplexExpression(IExpression lhs, int prev_op_prec, bool in_parens, int depth = 0) {
            var lookahead = GetBinaryOperator(CurToken);
            if (lookahead == null) return lhs;

            //Console.WriteLine($"{new string(' ', depth)}RCE: lhs = {lhs} lookahead = {lookahead.Value.TokenValue} prev_op_prec = {prev_op_prec}");

            if (lhs is FunctionCall) {
                ((FunctionCall)lhs).ForceTruncateReturnValues = false;
                // No need to force this (and produce extra parens),
                // because the binop truncates the return value anyway
            }

            while (lookahead.Value.Precedence >= prev_op_prec) {
                var op = lookahead;
                Move();
                var rhs = ReadSecondaryExpression();
                if (rhs is FunctionCall) {
                    ((FunctionCall)rhs).ForceTruncateReturnValues = false;
                }
                lookahead = GetBinaryOperator(CurToken);
                if (lookahead == null) return new BinaryOp(op.Value.BinaryOpType.Value, lhs, rhs);
                //Console.WriteLine($"{new string(' ', depth)}OUT rhs = {rhs} lookahead = {lookahead.Value.TokenValue} prec = {lookahead.Value.Precedence}");

                while (lookahead.Value.RightAssociative ? (lookahead.Value.Precedence == op.Value.Precedence) : (lookahead.Value.Precedence > op.Value.Precedence)) {
                    rhs = ReadComplexExpression(rhs, lookahead.Value.Precedence, in_parens, depth + 1);
                    //Console.WriteLine($"{new string(' ', depth)}IN rhs = {rhs} lookahead = {lookahead.Value.TokenValue}");
                    lookahead = GetBinaryOperator(CurToken);
                    if (lookahead == null) return new BinaryOp(op.Value.BinaryOpType.Value, lhs, rhs);
                }

                lhs = new BinaryOp(op.Value.BinaryOpType.Value, lhs, rhs);
            }

            return lhs;
        }

        /// <summary>
        /// Reads a single expression.
        /// </summary>
        /// <returns>The expression.</returns>
        public IExpression ReadExpression() {
            var expr = ReadSecondaryExpression();
            return ReadComplexExpression(expr, 0, false);
        }

        public Break ReadBreak() {
            if (!CurToken.IsPunctuation("break")) ThrowExpect("break statement", CurToken);
            Move();
            return new Break();
        }

        public Return ReadReturn() {
            if (!CurToken.IsPunctuation("return")) ThrowExpect("return statement", CurToken);
            Move();

            var ret_vals = new List<IExpression>();

            if (!CurToken.IsPunctuation("end")) {
                ret_vals.Add(ReadExpression());
            }

            while (CurToken.IsPunctuation(",")) {
                Move();
                ret_vals.Add(ReadExpression());
            }

            return new Return { Expressions = ret_vals };
        }

        public If ReadIf() {
            if (!CurToken.IsPunctuation("if")) ThrowExpect("if statement", CurToken);

            Move();

            var cond = ReadExpression();

            if (!CurToken.IsPunctuation("then")) ThrowExpect("'then' keyword", CurToken);
            Move();

            var statements = new List<IStatement>();

            while (!CurToken.IsPunctuation("else") && !CurToken.IsPunctuation("elseif") && !CurToken.IsPunctuation("end") && !CurToken.IsEOF()) {
                statements.Add(ReadStatement());
            }

            var mainif_cond_block = new ConditionalBlock {
                Block = new Block { Statements = statements },
                Condition = cond
            };

            var elseifs = new List<ConditionalBlock>();

            while (CurToken.IsPunctuation("elseif")) {
                Move();
                var elseif_cond = ReadExpression();
                if (!CurToken.IsPunctuation("then")) ThrowExpect("'then' keyword", CurToken);
                Move();
                var elseif_statements = new List<IStatement>();
                while (!CurToken.IsPunctuation("else") && !CurToken.IsPunctuation("elseif") && !CurToken.IsPunctuation("end") && !CurToken.IsEOF()) {
                    elseif_statements.Add(ReadStatement());
                }

                elseifs.Add(new ConditionalBlock {
                    Block = new Block { Statements = elseif_statements },
                    Condition = elseif_cond
                });
            }

            Block else_block = null;

            if (CurToken.IsPunctuation("else")) {
                Move();
                var else_statements = new List<IStatement>();
                while (!CurToken.IsPunctuation("end") && !CurToken.IsEOF()) {
                    else_statements.Add(ReadStatement());
                }

                else_block = new Block { Statements = else_statements };
            }

            if (!CurToken.IsPunctuation("end")) ThrowExpect("'end' keyword", CurToken);
            Move();

            return new If {
                MainIf = mainif_cond_block,
                ElseIfs = elseifs,
                Else = else_block
            };
        }

        public void SkipSemicolons() {
            while (CurToken.IsPunctuation(";")) Move();
        }

        public While ReadWhile() {
            if (!CurToken.IsPunctuation("while")) ThrowExpect("while statement", CurToken);

            Move();
            var cond = ReadExpression();

            if (!CurToken.IsPunctuation("do")) ThrowExpect("'do' keyword", CurToken);
            Move();

            SkipSemicolons();

            var statements = new List<IStatement>();

            while (!CurToken.IsPunctuation("end") && !CurToken.IsEOF()) {
                statements.Add(ReadStatement());
            }
            Move();

            return new While {
                Condition = cond,
                Block = new Block { Statements = statements }
            };
        }

        public Assignment TryReadFullAssignment(bool certain_assign, IExpression start_expr, Token expr_token) {
            // certain_assign should be set to true if we know that
            // what we have is definitely an assignment
            // that allows us to handle implicit nil assignments (local
            // declarations without a value) as an Assignment node

            if (certain_assign || (CurToken.IsPunctuation("=") || CurToken.IsPunctuation(","))) {
                if (!(start_expr is IAssignable)) ThrowExpect("assignable expression", expr_token);

                var assign_exprs = new List<IAssignable> { start_expr as IAssignable };

                while (CurToken.IsPunctuation(",")) {
                    Move();
                    start_expr = ReadExpression();
                    if (!(start_expr is IAssignable)) ThrowExpect("assignable expression", expr_token);

                    assign_exprs.Add(start_expr as IAssignable);
                }

                if (certain_assign && !CurToken.IsPunctuation("=")) {
                    // implicit nil assignment/local declaration

                    var local_decl = new Assignment {
                        IsLocal = true,
                        Targets = assign_exprs
                    };

                    if (ParserSettings.AutofillValuesInLocalDeclaration) {
                        // Match Values with NilLiterals
                        for (var i = 0; i < assign_exprs.Count; i++) {
                            local_decl.Values.Add(NilLiteral.Instance);
                        }
                    }

                    return local_decl;
                }

                return ReadAssignment(assign_exprs);
            }



            return null;
        }

        public Assignment ReadAssignment(List<IAssignable> assignable_exprs, bool local = false) {
            if (!CurToken.IsPunctuation("=")) ThrowExpect("assignment", CurToken);
            Move();
            var value_exprs = new List<IExpression> { ReadExpression() };

            while (CurToken.IsPunctuation(",")) {
                Move();
                value_exprs.Add(ReadExpression());
            }

            return new Assignment {
                IsLocal = local,
                Targets = assignable_exprs,
                Values = value_exprs,
            };
        }

        public Assignment ReadNamedFunctionDefinition() {
            if (!CurToken.IsPunctuation("function")) ThrowExpect("function", CurToken);
            Move();
            if (CurToken.Type != TokenType.Identifier) ThrowExpect("identifier", CurToken);
            IAssignable expr = new Variable { Name = CurToken.Value };
            Move();
            while (CurToken.IsPunctuation(".")) {
                Move();
                if (CurToken.Type != TokenType.Identifier) ThrowExpect("identifier", CurToken);
                expr = new TableAccess {
                    Table = expr as IExpression,
                    Index = new StringLiteral { Value = CurToken.Value }
                };
                Move();
            }
            var is_method_def = false;
            if (CurToken.IsPunctuation(":")) {
                is_method_def = true;
                Move();
                if (CurToken.Type != TokenType.Identifier) ThrowExpect("identifier", CurToken);
                expr = new TableAccess {
                    Table = expr as IExpression,
                    Index = new StringLiteral { Value = CurToken.Value }
                };
                Move();
            }
            var func_def = ReadFunctionDefinition(start_from_params: true, self: is_method_def);
            return new Assignment {
                Targets = new List<IAssignable> { expr },
                Values = new List<IExpression> { func_def }
            };
        }

        public Repeat ReadRepeat() {
            if (!CurToken.IsPunctuation("repeat")) ThrowExpect("repeat statement", CurToken);
            Move();
            SkipSemicolons();
            var statements = new List<IStatement>();
            while (!CurToken.IsPunctuation("until") && !CurToken.IsEOF()) {
                statements.Add(ReadStatement());
            }

            if (!CurToken.IsPunctuation("until")) ThrowExpect("'until' keyword", CurToken);
            Move();

            var cond = ReadExpression();

            return new Repeat {
                Condition = cond,
                Block = new Block { Statements = statements }
            };
        }

        public Block ReadBlock(bool alone = false) {
            if (!CurToken.IsPunctuation("do")) ThrowExpect("block", CurToken);
            Move();
            SkipSemicolons();

            var statements = new List<IStatement>();
            while (!CurToken.IsPunctuation("end") && !CurToken.IsEOF()) {
                statements.Add(ReadStatement());
            }

            Move();

            return new Block { Statements = statements };
        }

        public GenericFor ReadGenericFor() {
            if (CurToken.Type != TokenType.Identifier) ThrowExpect("identifier", CurToken);

            var var_names = new List<string> { CurToken.Value };
            Move();

            while (CurToken.IsPunctuation(",")) {
                Move();
                if (CurToken.Type != TokenType.Identifier) ThrowExpect("identifier", CurToken);
                var_names.Add(CurToken.Value);
                Move();
            }

            if (!CurToken.IsPunctuation("in")) ThrowExpect("'in' keyword", CurToken);
            Move();

            var iterator = ReadExpression();
            var block = ReadBlock();

            return new GenericFor {
                VariableNames = var_names,
                Iterator = iterator,
                Block = block
            };
        }

        public NumericFor ReadNumericFor() {
            if (CurToken.Type != TokenType.Identifier) ThrowExpect("identifier", CurToken);

            var var_name = CurToken.Value;
            Move();

            if (!CurToken.IsPunctuation("=")) ThrowExpect("assignment", CurToken);
            Move();

            var start_pos = ReadExpression();
            if (!CurToken.IsPunctuation(",")) ThrowExpect("end point expression", CurToken);
            Move();
            var end_pos = ReadExpression();

            IExpression step = null;
            if (CurToken.IsPunctuation(",")) {
                Move();
                step = ReadExpression();
            }

            if (step == null && ParserSettings.AutofillNumericForStep) {
                step = new NumberLiteral { Value = 1 };
            }

            var block = ReadBlock();

            return new NumericFor {
                VariableName = var_name,
                StartPoint = start_pos,
                EndPoint = end_pos,
                Step = step,
                Block = block
            };
        }

        public For ReadFor() {
            if (!CurToken.IsPunctuation("for")) ThrowExpect("for statement", CurToken);

            Move();

            var peek = PeekToken;
            if (peek.IsPunctuation(",") || peek.IsPunctuation("in")) {
                return ReadGenericFor();
            } else {
                return ReadNumericFor();
            }
        }

        public IStatement ReadPrimaryStatement() {
            if (CurToken.IsPunctuation("break")) {
                return ReadBreak();
            }

            if (CurToken.IsPunctuation("return")) {
                return ReadReturn();
            }

            if (CurToken.IsPunctuation("if")) {
                return ReadIf();
            }

            if (CurToken.IsPunctuation("while")) {
                return ReadWhile();
            }

            if (CurToken.IsPunctuation("function")) {
                return ReadNamedFunctionDefinition();
            }

            if (CurToken.IsPunctuation("repeat")) {
                return ReadRepeat();
            }

            if (CurToken.IsPunctuation("for")) {
                return ReadFor();
            }

            if (CurToken.IsPunctuation("do")) {
                return ReadBlock(alone: true);
            }

            if (CurToken.IsPunctuation("local")) {
                Move();
                if (CurToken.IsPunctuation("function")) {
                    var local_assign = ReadNamedFunctionDefinition();
                    local_assign.IsLocal = true;
                    return local_assign;
                } else {
                    var local_expr_token = CurToken;
                    var local_expr = ReadExpression();
                    var local_assign = TryReadFullAssignment(true, local_expr, local_expr_token);
                    if (local_assign == null) ThrowExpect("assignment statement", CurToken);
                    local_assign.IsLocal = true;
                    return local_assign;
                }
            }

            var expr_token = CurToken;
            var expr = ReadExpression();
            var assign = TryReadFullAssignment(false, expr, expr_token);
            if (assign != null) return assign;

            if (expr is FunctionCall) {
                return expr as FunctionCall;
            }

            ThrowExpect("statement", expr_token);
            throw new Exception("unreachable");
        }

        /// <summary>
        /// Reads a single statement.
        /// </summary>
        /// <returns>The statement.</returns>
        public IStatement ReadStatement() {
            var stat = ReadPrimaryStatement();
            SkipSemicolons();
            return stat;
        }

        /// <summary>
        /// Reads a list of statements.
        /// </summary>
        /// <returns>`Block` node (`TopLevel` = `true`).</returns>
        public Block Read() {
            var statements = new List<IStatement>();

            while (!CurToken.IsEOF()) {
                statements.Add(ReadStatement());
            }

            return new Block { Statements = statements, TopLevel = true };
        }
    }
}
