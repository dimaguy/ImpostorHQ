using ImpostorHQ.Core.Commands;
using ImpostorHQ.Tests.Mocks;
using NUnit.Framework;

namespace ImpostorHQ.Tests
{
    public class CommandTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestParser()
        {
            var parser = new CommandParser<MockCommand>();

            var c1 = new MockCommand("/test1", null, 0);
            var c2 = new MockCommand("/test2", null, 1);
            var c3 = new MockCommand("/test3", null, 2);

            parser.Register(c1);
            parser.Register(c2);
            parser.Register(c3);

            var result = parser.TryParse("/test_invalid");
            Assert.AreEqual(ParseStatus.UnknownCommand, result.Error, "Unknown no token command.");

            result = parser.TryParse("  ");
            Assert.AreEqual(ParseStatus.WhiteSpace, result.Error, "White space input.");

            result = parser.TryParse("/test1 abc");
            Assert.AreEqual(ParseStatus.InvalidSyntax, result.Error, "Arguments for plain command.");

            result = parser.TryParse("/test1");
            Assert.AreEqual(ParseStatus.None, result.Error, "Should have parsed test1 successfully.");
            Assert.AreEqual(c1, result.Command);


            result = parser.TryParse("/test2");
            Assert.AreEqual(ParseStatus.NoData, result.Error);

            result = parser.TryParse("/test2 abc def");
            Assert.AreEqual(ParseStatus.InvalidSyntax, result.Error);

            result = parser.TryParse("/test2 abc");
            Assert.AreEqual(ParseStatus.None, result.Error);
            Assert.AreEqual(c2, result.Command);
            Assert.AreEqual("abc", result.Tokens![0]);

            result = parser.TryParse("/test3 abc def");
            Assert.AreEqual(ParseStatus.None, result.Error);
            Assert.AreEqual(c3, result.Command);
            Assert.AreEqual("abc", result.Tokens![0]);
            Assert.AreEqual("def", result.Tokens![1]);
        }
    }
}