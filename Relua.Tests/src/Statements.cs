using System;
using NUnit.Framework;

namespace Relua.Tests {
    [TestFixture]
    public class Statements {
        [Test]
        public void DoubleBreak() {
            var tokenizer = new Tokenizer("break break");
            var parser = new Parser(tokenizer);
            var stat = parser.ReadStatement(); // only reads one statement!
            Assert.AreEqual("break", stat.ToString());
            var stat2 = parser.ReadStatement();
            Assert.AreEqual("break", stat2.ToString());
        }

        [Test]
        public void WhileStatement() {
            var tokenizer = new Tokenizer("while true do; break; end");
            var parser = new Parser(tokenizer);
            var stat = parser.ReadStatement() as AST.Node;
            Assert.AreEqual("while true do break end", stat.ToString(one_line: true));
        }

        [Test]
        public void IfStatement() {
            var tokenizer = new Tokenizer("if 3+3 then break elseif 2+2 then break else break end");
            var parser = new Parser(tokenizer);
            var stat = parser.ReadStatement() as AST.Node;
            Assert.AreEqual("if (3 + 3) then break elseif (2 + 2) then break else break end", stat.ToString(one_line: true));
        }

        [Test]
        public void Assignment() {
            var tokenizer = new Tokenizer("a, b = 3 * 3, 2");
            var parser = new Parser(tokenizer);
            var stat = parser.ReadStatement() as AST.Node;
            Assert.AreEqual("a, b = (3 * 3), 2", stat.ToString(one_line: true));
        }

        [Test]
        public void LocalFunctionCall() {
            var tokenizer = new Tokenizer("local a, b = 'a', 'b'");
            var parser = new Parser(tokenizer);
            var stat = parser.ReadStatement() as AST.Node;
            Assert.AreEqual("local a, b = \"a\", \"b\"", stat.ToString(one_line: true));
        }

        [Test]
        public void FunctionCall() {
            var tokenizer = new Tokenizer("test('a') test('b', 3)");
            var parser = new Parser(tokenizer);
            var stat = parser.ReadStatement() as AST.Node;
            Assert.AreEqual("test(\"a\")", stat.ToString(one_line: true));
            stat = parser.ReadStatement() as AST.Node;
            Assert.AreEqual("test(\"b\", 3)", stat.ToString(one_line: true));
        }

        [Test]
        public void FunctionDefinition() {
            var tokenizer = new Tokenizer("function test(a, b) print(a); print(b); end");
            var parser = new Parser(tokenizer);
            var stat = parser.ReadStatement() as AST.Node;
            Assert.AreEqual("function test(a, b) print(a) print(b) end", stat.ToString(one_line: true));
        }
    }
}
