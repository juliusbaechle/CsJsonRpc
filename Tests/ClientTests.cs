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
            var mock = new ActiveSocketMock();
            var client = new Client(mock);

            // Request, No parameters
            int result = 0;
            mock.Response = """{"id":0, "jsonrpc":"2.0", "result":1}""";
            client.Request<int>("startOrder", (int id) => { result = id; });
            Assert.AreEqual("""{"jsonrpc":"2.0","method":"startOrder","id":0}""", mock.CapturedRequest);
            Assert.AreEqual(1, result);

            // Request, Unnamed parameters
            mock.Response = """{"id":1, "jsonrpc":"2.0", "result":1}""";
            result = client.Request<int>("startOrder", new JsonArray { 1 }, (int id) => { result = id; });
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
        public async Task ReceivesExceptions()
        {
            var mock = new ActiveSocketMock();
            var client = new Client(mock);

            Exception? ex = null;
            var jsonRpcEx = new JsonRpcException(JsonRpcException.ErrorCode.internal_error, "exception occured");
            mock.Response = """{"id":0, "jsonrpc":"2.0", "error":""" + JsonSerializer.Serialize(jsonRpcEx) + "}";
            client.Request<int>("startOrder", (r) => { }, (e) => { ex = e; });
            Assert.IsNotNull(ex);
        }
        // 
        // [TestMethod]
        // public async Task HandlesInvalidResponses()
        // {
        //     var mock = new ActiveSocketMock();
        //     var client = new Client(mock);
        // 
        //     mock.Response = "(XXX---XXX)";
        //     Assert.Throws<JsonRpcException>(() => client.Request<int>(1, "method"));
        // 
        //     mock.Response = """null""";
        //     Assert.Throws<JsonRpcException>(() => client.Request<int>(1, "method"));
        // 
        //     mock.Response = """[1, 2, 3]""";
        //     Assert.Throws<JsonRpcException>(() => client.Request<int>(1, "method"));
        // 
        //     mock.Response = """{"id":1, "jsonrpc":"2.0"}""";
        //     Assert.Throws<JsonRpcException>(() => client.Request<int>(1, "method"));
        // 
        //     mock.Response = """{"id":1, "error":"dummy"}""";
        //     Assert.Throws<JsonRpcException>(() => client.Request<int>(1, "method"));
        // 
        //     mock.Response = """{"id":1, "jsonrpc":"2.0", "result":"invalid result"}""";
        //     Assert.Throws<JsonRpcException>(() => client.Request<int>(1, "method"));
        // }

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
