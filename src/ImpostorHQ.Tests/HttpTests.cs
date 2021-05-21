using System.IO;
using System.Text;
using ImpostorHQ.Http;
using NUnit.Framework;

namespace ImpostorHQ.Tests
{
    public class HttpTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestMaxReadLength()
        {
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes("test data\r\n"));
            Assert.AreEqual(ms.ReadLineSized(4).Result, "test");
            Assert.Pass();
        }

        [Test]
        public void TestReadNewline()
        {
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes("test data\r\n"));
            Assert.AreEqual(ms.ReadLineSized(1024).Result, "test data\r\n");
            Assert.Pass();
        }

        [Test]
        public void TestParseMalformed()
        {
            Assert.IsNull(HttpParser.ParseRequest("GET HTTP/1.1"));
            Assert.IsNull(HttpParser.ParseRequest("GET /HTTP/1.1"));
            Assert.IsNull(HttpParser.ParseRequest("GET/ HTTP/1.1"));
            Assert.IsNull(HttpParser.ParseRequest("GET / HTTP"));
        }

        [Test]
        public void TestParseGet()
        {
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes("GET / HTTP/1.1"));
            var request = HttpParser.ParseRequest(ms.ReadLineSized(1024).Result);
            Assert.IsNotNull(request);
            Assert.AreEqual(request.Value.Path, "/");
            Assert.AreEqual(request.Value.Method, HttpRequestMethod.GET);
        }

        [Test]
        public void TestParseHead()
        {
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes("HEAD / HTTP/1.1"));
            var request = HttpParser.ParseRequest(ms.ReadLineSized(1024).Result);
            Assert.IsNotNull(request);
            Assert.AreEqual(request.Value.Path, "/");
            Assert.AreEqual(request.Value.Method, HttpRequestMethod.HEAD);
        }
    }
}