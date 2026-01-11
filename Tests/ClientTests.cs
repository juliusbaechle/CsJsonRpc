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
            var mock = new ServerMock();
            var client = new Client(mock, new());

            // Void-Request, No parameters
            mock.Response = """{"id":0, "jsonrpc":"2.0", "result":null}""";
            await client.Request("startOrder");
            Assert.AreEqual("""{"jsonrpc":"2.0","method":"startOrder","params":null,"id":0}""", mock.CapturedRequest);

            // Void-Request where result is not null
            mock.Response = """{"id":1, "jsonrpc":"2.0", "result":"not null result"}""";
            await client.Request("method");

            // Int-Request, Unnamed parameters
            mock.Response = """{"id":2, "jsonrpc":"2.0", "result":1}""";
            var result = await client.Request<int>("startOrder", new JsonArray([1]));
            Assert.AreEqual("""{"jsonrpc":"2.0","method":"startOrder","params":[1],"id":2}""", mock.CapturedRequest);
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
            var mock = new ServerMock();
            var client = new Client(mock, new());

            var jsonRpcEx = new JsonRpcException(JsonRpcException.ErrorCode.internal_error, "exception occured");
            mock.Response = """{"id":0, "jsonrpc":"2.0", "error":""" + JsonSerializer.Serialize(jsonRpcEx) + "}";
            await Assert.ThrowsAsync<JsonRpcException>(async () => await client.Request<int>("startOrder"));

            jsonRpcEx = new JsonRpcException(JsonRpcException.ErrorCode.internal_error, "exception occured");
            mock.Response = """{"id":1, "jsonrpc":"2.0", "error":""" + JsonSerializer.Serialize(jsonRpcEx) + "}";
            await Assert.ThrowsAsync<JsonRpcException>(async () => await client.Request("startOrder"));
        }

        [TestMethod]
        public async Task HandlesInvalidResponses()
        {
            var mock = new ServerMock();
            var client = new Client(mock, new());
        
            mock.Response = "(XXX---XXX)";
            client.Request<int>("method");

            mock.Response = "null";
            client.Request("method");

            mock.Response = """{"id":2, "jsonrpc":"2.0"}""";
            await Assert.ThrowsAsync<JsonRpcException>(async () => await client.Request<int>("method"));
        
            mock.Response = """{"id":3, "error":"dummy"}""";
            await Assert.ThrowsAsync<JsonRpcException>(async () => await client.Request<int>("method"));
        
            mock.Response = """{"id":4, "jsonrpc":"2.0", "result":"invalid result"}""";
            await Assert.ThrowsAsync<JsonException>(async () => await client.Request<int>("method"));

            client.Dispose();
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
