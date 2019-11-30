using System;
using NUnit.Framework;

namespace Relua.Tests {
    [TestFixture]
    public class FunctionCalls {
        [Test]
        public void NoArgsFunctionCall() {
            var tokenizer = new Tokenizer("print()");
            var parser = new Parser(tokenizer);
            var expr = parser.ReadExpression();
            Assert.AreEqual("print()", expr.ToString());
        }

        [Test]
        public void SimpleFunctionCall() {
            var tokenizer = new Tokenizer("print('Hello, world!')");
            var parser = new Parser(tokenizer);
            var expr = parser.ReadExpression();
            Assert.AreEqual("print(\"Hello, world!\")", expr.ToString());
        }

        [Test]
        public void ComplexFunctionCall() {
            var tokenizer = new Tokenizer("print('Hello, world!', 3 + 4, a.b)");
            var parser = new Parser(tokenizer);
            var expr = parser.ReadExpression();
            Assert.AreEqual("print(\"Hello, world!\", (3 + 4), a.b)", expr.ToString());
        }

        [Test]
        public void FancyStringFunctionCall() {
            var tokenizer = new Tokenizer("print'Hello, world!'");
            var parser = new Parser(tokenizer);
            var expr = parser.ReadExpression();
            Assert.AreEqual("print(\"Hello, world!\")", expr.ToString());
        }

        [Test]
        public void FancyTableFunctionCall() {
            var tokenizer = new Tokenizer("print{1, 2}");
            var parser = new Parser(tokenizer);
            var expr = parser.ReadExpression() as AST.Node;
            Assert.AreEqual("print({ 1, 2 })", expr.ToString(one_line: true));
        }
    }
}
