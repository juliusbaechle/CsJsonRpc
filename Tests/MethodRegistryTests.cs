using JsonRpc;
using System;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Tests
{

    [TestClass]
    public sealed class MethodRegistryTests
    {
        class Calculator
        {
            public void Print(int value) { Console.WriteLine(value); }
            public int Increment(int value) { return value + 1; }
            public Task PrintAsync(int value) { return Task.Run(() => { Console.WriteLine(value); }); }
            public Task<int> IncrementAsync(int value) { return Task.Run(() => { return value + 1; }); }
        }

        [TestMethod]
        public async Task AddAndRemoveMethod()
        {
            Calculator calculator = new();
            MethodRegistry registry = new();

            registry.Add("Print", calculator.Print);
            Assert.IsTrue(registry.Contains("Print"));
            Assert.Throws<JsonRpcException>(() => registry.Add("Print", calculator.Print));

            registry.Remove("Print");
            Assert.IsFalse(registry.Contains("Print"));
            Assert.Throws<JsonRpcException>(() => registry.Remove("Print"));
        }

        [TestMethod]
        public async Task ProcessMethods()
        {
            Calculator calculator = new();
            MethodRegistry registry = new();
            registry.Add("Print", calculator.Print);
            registry.Add("Increment", calculator.Increment, [ "Value" ]);
            registry.Add("PrintAsync", calculator.PrintAsync);
            registry.Add("IncrementAsync", calculator.IncrementAsync);

            var unnamed_params = JsonDocument.Parse("""[1]""");
            var named_params = JsonDocument.Parse("""{"Value": 1}""");
            var too_few_params = JsonDocument.Parse("[]");
            var too_many_params = JsonDocument.Parse("[2, 3]");
            var wrong_param_type = JsonDocument.Parse("""["abc"]""");

            var result = await registry.Process("Increment", unnamed_params);
            Assert.AreEqual<int>(2, JsonSerializer.Deserialize<int>(result));

            result = await registry.Process("Increment", named_params);
            Assert.AreEqual<int>(2, JsonSerializer.Deserialize<int>(result));

            result = await registry.Process("IncrementAsync", unnamed_params);
            Assert.AreEqual<int>(2, JsonSerializer.Deserialize<int>(result));

            result = await registry.Process("Print", unnamed_params);
            Assert.AreEqual<string>("null", result);
            await Assert.ThrowsAsync<JsonRpcException>(() => registry.Process("Print", named_params));
            await Assert.ThrowsAsync<JsonRpcException>(() => registry.Process("Print", too_few_params));
            await Assert.ThrowsAsync<JsonRpcException>(() => registry.Process("Print", too_many_params));
            await Assert.ThrowsAsync<JsonRpcException>(() => registry.Process("Print", wrong_param_type));

            result = await registry.Process("PrintAsync", unnamed_params);
            Assert.AreEqual<string>("null", result);
            await Assert.ThrowsAsync<JsonRpcException>(() => registry.Process("PrintAsync", named_params));
            await Assert.ThrowsAsync<JsonRpcException>(() => registry.Process("PrintAsync", too_few_params));
            await Assert.ThrowsAsync<JsonRpcException>(() => registry.Process("PrintAsync", too_many_params));
        }
    }
}
