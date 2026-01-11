using JsonRpc;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Tests {
    [TestClass]
    public sealed class JsonRpcExceptionTests {
        [TestMethod]
        public async Task ValidJson() {
            var json = new JsonObject { { "code", -32603 }, { "message", "internal_error" }, { "data", new JsonObject { } } };
            JsonRpcException ex = json;
            JsonNode node = ex;
        }

        [TestMethod]
        public async Task InvalidJson() {
            var json = new JsonObject { { "code", -32603 }, { "message", null } };
            JsonRpcException ex = json;

            json = new JsonObject { { "code", null }, { "message", "hello" } };
            ex = json;

            json = new JsonObject { { "code", -32603 }, { "message", 3 } };
            ex = json;

            json = null;
            ex = json;
        }
    }
}
