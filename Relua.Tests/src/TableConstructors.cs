using System;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace Relua.Tests {
    [TestFixture]
    public class TableConstructors {
        [Test]
        public void EmptyTableCtor() {
            var tokenizer = new Tokenizer("{  }");
            var parser = new Parser(tokenizer);
            var expr = parser.ReadExpression();
            Assert.AreEqual("{}", expr.ToString());
        }

        [Test]
        public void SingleElementTableCtor() {
            var tokenizer = new Tokenizer("{ 123 }");
            var parser = new Parser(tokenizer);
            var expr = parser.ReadExpression();
            Assert.AreEqual("{ 123 }", expr.ToString());
        }

        [Test]
        public void SingleIndexedElementTableCtor() {
            var tokenizer = new Tokenizer("{ a = 123 }");
            var parser = new Parser(tokenizer);
            var expr = parser.ReadExpression();
            Assert.AreEqual("{ a = 123 }", expr.ToString());
        }

        [Test]
        public void SingleExpressionIndexedElementTableCtor() {
            var tokenizer = new Tokenizer("{ [1 + 2] = 123 }");
            var parser = new Parser(tokenizer);
            var expr = parser.ReadExpression();
            Assert.AreEqual("{ [(1 + 2)] = 123 }", expr.ToString());
        }

        [Test]
        public void MultiElementTableCtor() {
            var tokenizer = new Tokenizer("{ 1, 2 }");
            var parser = new Parser(tokenizer);
            var expr = parser.ReadExpression();
            Assert.AreEqual("{\n    1,\n    2\n}", expr.ToString());
        }

        [Test]
        public void MultiElementTableCtorOneLine() {
            var tokenizer = new Tokenizer("{ 1, 2 }");
            var parser = new Parser(tokenizer);
            var expr = parser.ReadExpression();
            var s = new StringBuilder();
            var sw = new StringWriter(s);
            var iw = new IndentAwareTextWriter(sw);
            iw.ForceOneLine = true;
            expr.Write(iw);
            Assert.AreEqual("{ 1, 2 }", s.ToString());
        }

        [Test]
        public void MultiElementTableCtorExplicitKeys() {
            var tokenizer = new Tokenizer("{ 1, 2 }");
            var parser = new Parser(tokenizer);
            var expr = parser.ReadExpression() as AST.TableConstructor;
            expr.Entries[0].ExplicitKey = true;
            expr.Entries[1].ExplicitKey = true;
            Assert.AreEqual("{ [1] = 1, [2] = 2 }", expr.ToString(one_line: true));
        }

        [Test]
        public void MultiElementTableCtorExplicitKeysConfusing() {
            var tokenizer = new Tokenizer("{ 1, [2] = 3, 2 }");
            var parser = new Parser(tokenizer);
            var expr = parser.ReadExpression() as AST.TableConstructor;
            expr.Entries[0].ExplicitKey = true;
            expr.Entries[2].ExplicitKey = true;
            Assert.AreEqual("{ [1] = 1, [2] = 3, [2] = 2 }", expr.ToString(one_line: true));
        }

        [Test]
        public void MultiElementTableCtorExpressions() {
            var tokenizer = new Tokenizer("{ 1 + 3, a.b.c }");
            var parser = new Parser(tokenizer);
            var expr = parser.ReadExpression() as AST.TableConstructor;
            Assert.AreEqual("{ (1 + 3), a.b.c }", expr.ToString(one_line: true));
        }
    }
}
