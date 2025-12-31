using JsonRpc;
using System.Text.Json;
using System.Text.Json.Nodes;
using Tests.Mocks;

namespace Tests
{

    [TestClass]
    public sealed class RequestProcessorTests
    {
        [TestMethod]
        public async Task CallMethods()
        {
            var registry = new MethodRegistry();
            var processor = new RequestProcessor(registry);
            processor.Add("Subtract", (int minuend, int subtrahend) => { return minuend - subtrahend; }, ["Minuend", "Subtrahend"]);
            
            var request = JsonSerializer.Serialize(JsonBuilders.Request(null, "Subtract", new JsonObject { { "Minuend", 3 }, { "Subtrahend", 1 } }));
            var response = processor.HandleRequest(request);
            var expc_response = JsonSerializer.Serialize(JsonBuilders.Response(null, 2));
            Assert.AreEqual(expc_response, response);

            request = JsonSerializer.Serialize(JsonBuilders.Request("hello", "Subtract", new JsonObject { { "Minuend", 3 }, { "Subtrahend", 1 } }));
            response = processor.HandleRequest(request);
            expc_response = JsonSerializer.Serialize(JsonBuilders.Response("hello", 2));
            Assert.AreEqual(expc_response, response);
        }

        [TestMethod]
        public async Task CallMethodWithNullValue()
        {
            var registry = new MethodRegistry();
            var processor = new RequestProcessor(registry);
            processor.Add("HandleNull", (JsonObject? obj) => { return; }, ["Object"]);

            var request = JsonSerializer.Serialize(JsonBuilders.Request(1, "HandleNull", new JsonObject { { "Object", null } }));
            var response = processor.HandleRequest(request);
            var expc_response = JsonSerializer.Serialize(JsonBuilders.Response(1, (JsonNode) null));
            Assert.AreEqual(expc_response, response);
        }

        [TestMethod]
        public async Task CallNotification()
        {
            var registry = new MethodRegistry();
            var processor = new RequestProcessor(registry);

            List<int> recv_params = [];
            processor.Add("SetParam", (List<int> a_params) => { recv_params = a_params; });
            var sent_params = new List<int>([1, 2, 3, 4]);
            var request = JsonSerializer.Serialize(JsonBuilders.Notify("SetParam", new JsonArray([JsonSerializer.SerializeToNode(sent_params)])));
            var response = processor.HandleRequest(request);
            Assert.AreEqual("null", response);
            Assert.HasCount(4, recv_params);
        }

        [TestMethod]
        public async Task InvalidRequests()
        {
            var registry = new MethodRegistry();
            var processor = new RequestProcessor(registry);

            // Method not found
            var request = JsonSerializer.Serialize(JsonBuilders.Request(1, "foobar"));
            var json = JsonDocument.Parse(processor.HandleRequest(request)).Deserialize<JsonObject>();
            Assert.AreEqual(JsonRpcException.ErrorCode.method_not_found, json["error"]["code"].Deserialize<JsonRpcException.ErrorCode>());

            // Parse error
            request = """{"jsonrpc": "2.0", "method":"foobar", "params": "bar", "baz]""";
            json = JsonDocument.Parse(processor.HandleRequest(request)).Deserialize<JsonObject>();
            Assert.AreEqual(JsonRpcException.ErrorCode.parse_error, json["error"]["code"].Deserialize<JsonRpcException.ErrorCode>());

            // Invalid method
            request = """{"jsonrpc": "2.0", "method":1}""";
            json = JsonDocument.Parse(processor.HandleRequest(request)).Deserialize<JsonObject>();
            Assert.AreEqual(JsonRpcException.ErrorCode.invalid_request, json["error"]["code"].Deserialize<JsonRpcException.ErrorCode>());
            
            // Missing jsonrpc
            request = """{"method":"foobar"}""";
            json = JsonDocument.Parse(processor.HandleRequest(request)).Deserialize<JsonObject>();
            Assert.AreEqual(JsonRpcException.ErrorCode.invalid_request, json["error"]["code"].Deserialize<JsonRpcException.ErrorCode>());

            // Invalid params
            request = """{"jsonrpc":"2.0", "method":"foobar", "params":"bar"}""";
            json = JsonDocument.Parse(processor.HandleRequest(request)).Deserialize<JsonObject>();
            Assert.AreEqual(JsonRpcException.ErrorCode.invalid_request, json["error"]["code"].Deserialize<JsonRpcException.ErrorCode>());

            // Invalid id
            request = """{"id": [1, 2], "jsonrpc":"2.0", "method":"foobar"}""";
            json = JsonDocument.Parse(processor.HandleRequest(request)).Deserialize<JsonObject>();
            Assert.AreEqual(JsonRpcException.ErrorCode.invalid_request, json["error"]["code"].Deserialize<JsonRpcException.ErrorCode>());

            // Invalid jsonrpc field
            request = """{"jsonrpc":null, "method":"foobar"}""";
            json = JsonDocument.Parse(processor.HandleRequest(request)).Deserialize<JsonObject>();
            Assert.AreEqual(JsonRpcException.ErrorCode.invalid_request, json["error"]["code"].Deserialize<JsonRpcException.ErrorCode>());

            // Invalid request
            request = "\"Hello World!\"";
            json = JsonDocument.Parse(processor.HandleRequest(request)).Deserialize<JsonObject>();
            Assert.AreEqual(JsonRpcException.ErrorCode.invalid_request, json["error"]["code"].Deserialize<JsonRpcException.ErrorCode>());
        }

        [TestMethod]
        public async Task ExceptionInMethod()
        {
            var registry = new MethodRegistry();
            var processor = new RequestProcessor(registry);
            processor.Add("ThrowException", () => { throw new ArgumentNullException(); });

            // Method not found
            var request = JsonSerializer.Serialize(JsonBuilders.Request(1, "ThrowException"));
            var json = JsonDocument.Parse(processor.HandleRequest(request)).Deserialize<JsonObject>();
            Assert.AreEqual(JsonRpcException.ErrorCode.exception_encoding_failed, json["error"]["code"].Deserialize<JsonRpcException.ErrorCode>());
        }

        [TestMethod]
        public async Task HandleBatchRequest()
        {
            var registry = new MethodRegistry();
            var processor = new RequestProcessor(registry);
            processor.Add("Add", (int a, int b) => { return a + b; });
            processor.Add("Increment", (int a) => { return a + 1; });

            var req = new JsonArray();
            req.Add(JsonBuilders.Request(1, "Add", new JsonArray([1, 2])));
            req.Add(JsonBuilders.Request(1, "Increment", new JsonArray([1])));
            var req_str = JsonSerializer.Serialize(req);

            var json = JsonDocument.Parse(processor.HandleRequest(req_str)).Deserialize<JsonArray>();
            Assert.IsTrue(JsonNode.DeepEquals(json[0], JsonBuilders.Response(1, 3)));
            Assert.IsTrue(JsonNode.DeepEquals(json[1], JsonBuilders.Response(1, 2)));
        }
    }
}
