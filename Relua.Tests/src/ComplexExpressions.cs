using NUnit.Framework;
using System;
using Relua;
namespace Relua.Tests {
    [TestFixture]
    public class Expressions {
        [Test]
        public void BasicAddition() {
            var tokenizer = new Tokenizer("abc + def");
            var parser = new Parser(tokenizer);
            var expr = parser.ReadExpression();
            Assert.AreEqual("(abc + def)", expr.ToString());
        }

        [Test]
        public void Precedence() {
            var tokenizer = new Tokenizer("a + b * c");
            var parser = new Parser(tokenizer);
            var expr = parser.ReadExpression();
            Assert.AreEqual("(a + (b * c))", expr.ToString());
        }

        [Test]
        public void RightAssociative() {
            var tokenizer = new Tokenizer("a + b * c ^ d ^ e .. f .. g");
            var parser = new Parser(tokenizer);
            var expr = parser.ReadExpression();
            Assert.AreEqual("((a + ((b * c) ^ (d ^ e))) .. (f .. g))", expr.ToString());
        }

        [Test]
        public void Parentheses() {
            var tokenizer = new Tokenizer("(a + b) * c");
            var parser = new Parser(tokenizer);
            var expr = parser.ReadExpression();
            Assert.AreEqual("((a + b) * c)", expr.ToString());
        }

        [Test]
        public void ParenthesisSpam() {
            var tokenizer = new Tokenizer("((a+((b*c)^(d^e)))..(f..g))");
            var parser = new Parser(tokenizer);
            var expr = parser.ReadExpression();
            Assert.AreEqual("((a + ((b * c) ^ (d ^ e))) .. (f .. g))", expr.ToString());
        }

        [Test]
        public void BasicUnary() {
            var tokenizer = new Tokenizer("a + -b + -(a + b)");
            var parser = new Parser(tokenizer);
            var expr = parser.ReadExpression();
            Assert.AreEqual("((a + (-b)) + (-(a + b)))", expr.ToString());
        }

        [Test]
        public void WordOperators() {
            var tokenizer = new Tokenizer("a + not b or c");
            var parser = new Parser(tokenizer);
            var expr = parser.ReadExpression();
            Assert.AreEqual("((a + (not b)) or c)", expr.ToString());
        }

        [Test]
        public void TableAccess() {
            var tokenizer = new Tokenizer("a.b.c");
            var parser = new Parser(tokenizer);
            var expr = parser.ReadExpression();
            Assert.IsInstanceOf(typeof(AST.TableAccess), expr);
            Assert.AreEqual("a.b.c", expr.ToString());
        }

        [Test]
        public void TableAccessOperations() {
            var tokenizer = new Tokenizer("a.b.c.d + a.e");
            var parser = new Parser(tokenizer);
            var expr = parser.ReadExpression();
            Assert.AreEqual("(a.b.c.d + a.e)", expr.ToString());
        }

        [Test]
        public void ExpressionTableAccess() {
            var tokenizer = new Tokenizer("a[\"not identifier\"] + a.e");
            var parser = new Parser(tokenizer);
            var expr = parser.ReadExpression();
            Assert.AreEqual("(a[\"not identifier\"] + a.e)", expr.ToString());
        }

        [Test]
        public void FunctionDefinition() {
            var tokenizer = new Tokenizer("function(a, b) break end");
            var parser = new Parser(tokenizer);
            var expr = parser.ReadExpression() as AST.Node;
            Assert.AreEqual("function(a, b) break end", expr.ToString(one_line: true));
        }

        [Test]
        public void SelfCall() {
            var tokenizer = new Tokenizer("a:test('hello')");
            var parser = new Parser(tokenizer);
            var expr = parser.ReadExpression() as AST.Node;
            Assert.AreEqual("a:test(\"hello\")", expr.ToString(one_line: true));
        }

        [Test]
        public void LuaJITLong() {
            var tokenizer = new Tokenizer("123LL");
            var parser = new Parser(tokenizer);
            parser.ParserSettings.EnableLuaJITLongs = true;
            var expr = parser.ReadExpression() as AST.Node;
            Assert.AreEqual("123LL", expr.ToString(one_line: true));
        }

        [Test]
        public void HexLiteral() {
            var tokenizer = new Tokenizer("0x123");
            var parser = new Parser(tokenizer);
            var expr = parser.ReadExpression() as AST.Node;
            Assert.AreEqual("0x123", expr.ToString(one_line: true));
        }

        [Test]
        public void SyntaxErrorCompatibility() {
            var parser = new Parser("\"ok\":match(\"ok\")");
            parser.ParserSettings.MaintainSyntaxErrorCompatibility = true;
            var ex = Assert.Throws<ParserException>(() => parser.ReadStatement());
            Assert.AreEqual("Failed parsing: syntax error compat: can't directly index strings, use parentheses [1:4]", ex.Message);
        }
    }
}
