using JsonRpc;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Tests
{
    [TestClass]
    public sealed class MethodRegistryTests
    {

        [TestMethod]
        public async Task AddAndRemoveMethods()
        {
            var registry = new MethodRegistry();
            registry.Add("StartOrder", () => { Console.WriteLine("Started Order"); });
            Assert.Throws<JsonRpcException>(() => { registry.Add("StartOrder", () => { }); });

            Assert.IsTrue(registry.Contains("StartOrder"));

            registry.Remove("StartOrder");
            Assert.Throws<JsonRpcException>(() => { registry.Remove("StartOrder"); });
        }

        [TestMethod]
        public async Task VoidMethod()
        {
            bool called = false;
            var registry = new MethodRegistry();
            registry.Add("VoidMethod", () => { called = true; });
            registry.Process("VoidMethod", null);
            Assert.IsTrue(called);
        }

        [TestMethod]
        public async Task Increment()
        {
            var registry = new MethodRegistry();
            registry.Add("Increment", (int i) => { return i + 1; });
            var result = registry.Process("Increment", new JsonArray([1]));
            Assert.AreEqual(2, result.Deserialize<int>());
            var parameters = new JsonObject{ { "Value", 1 } };
            Assert.Throws<JsonRpcException>(() => registry.Process("Increment", parameters));
        }

        [TestMethod]
        public async Task MethodWithNamedParams()
        {
            var registry = new MethodRegistry();
            registry.Add("Increment", (int i) => { return i + 1; }, [ "Value" ]);
            var parameters = new JsonObject { { "Value", 1 } };
            var result = registry.Process("Increment", parameters);
            Assert.AreEqual(2, result.Deserialize<int>());

            registry.Remove("Increment");
            Assert.Throws<JsonRpcException>(() => registry.Process("Increment", parameters));
        }

        [TestMethod]
        public async Task WrongParams()
        {
            var registry = new MethodRegistry();
            registry.Add("Increment", (int i) => { }, ["Value"]);
            Assert.Throws<JsonRpcException>(() => registry.Process("Increment", new JsonObject { { "WrongValues", 1 } }));
            Assert.Throws<JsonRpcException>(() => registry.Process("Increment", new JsonArray([])));
            Assert.Throws<JsonRpcException>(() => registry.Process("Increment", new JsonArray([1, 2])));
            Assert.Throws<JsonRpcException>(() => registry.Process("Increment", 2));
            Assert.Throws<JsonRpcException>(() => registry.Process("Increment", new JsonObject { { "Value", "1" } }));
            Assert.Throws<JsonRpcException>(() => registry.Process("Increment", new JsonObject { { "WrongValues", null } }));
        }
    }
}
