using JsonRpc;
using System.Text.Json;
using System.Text.Json.Nodes;
using Tests.Mocks;

namespace Tests
{

    [TestClass]
    public sealed class ClientTests
    {
        [TestMethod]
        public async Task CallMethods()
        {
            var mock = new ClientConnectorMock();
            var client = new Client(mock);
            
            // Request, No parameters
            mock.Response = """{"id":1, "jsonrpc":"2.0", "result":1}""";
            var result = client.Request<int>(1, "startOrder");
            Assert.AreEqual("""{"jsonrpc":"2.0","method":"startOrder","id":1}""", mock.CapturedRequest);
            Assert.AreEqual(1, result);

            // Request, Unnamed parameters
            mock.Response = """{"id":1, "jsonrpc":"2.0", "result":1}""";
            result = client.Request<int>(1, "startOrder", new JsonArray { 1 });
            Assert.AreEqual("""{"jsonrpc":"2.0","method":"startOrder","params":[1],"id":1}""", mock.CapturedRequest);
            Assert.AreEqual(1, result);

            // Notify, No parameters
            client.Notify("startOrder");
            Assert.AreEqual("""{"jsonrpc":"2.0","method":"startOrder"}""", mock.CapturedRequest);

            // Notify, Named parameters
            client.Notify("startOrder", new JsonObject { { "OrderId", 1 } });
            Assert.AreEqual("""{"jsonrpc":"2.0","method":"startOrder","params":{"OrderId":1}}""", mock.CapturedRequest);
        }

        [TestMethod]
        public async Task ThrowsExceptions()
        {
            var mock = new ClientConnectorMock();
            var client = new Client(mock);

            // string exception
            mock.Response = """{"id":1, "jsonrpc":"2.0", "error":"exception"}""";
            Assert.Throws<JsonRpcException>(() => client.Request<int>(1, "startOrder"));

            // jsonrpc exception
            var ex = new JsonRpcException(JsonRpcException.ErrorCode.internal_error, "exception occured");
            mock.Response = """{"id":1, "jsonrpc":"2.0", "error":""" + JsonSerializer.Serialize(ex) + "}";
            Assert.Throws<JsonRpcException>(() => client.Request<int>(1, "startOrder"));
        }

        [TestMethod]
        public async Task HandlesInvalidResponses()
        {
            var mock = new ClientConnectorMock();
            var client = new Client(mock);

            mock.Response = "(XXX---XXX)";
            Assert.Throws<JsonRpcException>(() => client.Request<int>(1, "method"));

            mock.Response = """null""";
            Assert.Throws<JsonRpcException>(() => client.Request<int>(1, "method"));

            mock.Response = """[1, 2, 3]""";
            Assert.Throws<JsonRpcException>(() => client.Request<int>(1, "method"));

            mock.Response = """{"id":1, "jsonrpc":"2.0"}""";
            Assert.Throws<JsonRpcException>(() => client.Request<int>(1, "method"));

            mock.Response = """{"id":1, "error":"dummy"}""";
            Assert.Throws<JsonRpcException>(() => client.Request<int>(1, "method"));

            mock.Response = """{"id":1, "jsonrpc":"2.0", "result":"invalid result"}""";
            Assert.Throws<JsonRpcException>(() => client.Request<int>(1, "method"));
        }

        [TestMethod]
        public async Task SupportsUtf8()
        {
            var json = new JsonObject { { "Name", "Julius Bächle" } };
            var str = JsonSerializer.Serialize(json);
            json = JsonDocument.Parse(str).Deserialize<JsonObject>();
            Assert.AreEqual("Julius Bächle", json["Name"].Deserialize<string>());
        }
    }
}
