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
    public sealed class MethodHandleTests
    {
        class Calculator
        {
            public int Increment(int value) { return value + 1; }
            public Task<int> IncrementAsync(int value) { return Task.Run(() => { return value + 1; }); }
            public Task Print(int value) { return Task.Run(() => { Console.WriteLine(value); }); }
        }

        [TestMethod]
        public async Task TestHandle()
        {
            Calculator calculator = new();
            MethodRegistry registry = new();
            registry.Add(calculator.Print);
            registry.Add(calculator.Increment);
            registry.Add(calculator.IncrementAsync);
            Assert.IsTrue(registry.Contains("Print"));
            Assert.IsTrue(registry.Contains("Increment"));
            Assert.IsTrue(registry.Contains("IncrementAsync"));

            var unnamed_params = JsonDocument.Parse("""[1]""");
            var result = registry.Process("Increment", unnamed_params).Result;
            Assert.AreEqual<int>(JsonSerializer.Deserialize<int>(result), 2);

            var named_params = JsonDocument.Parse("""{"value": 1}""");
            result = registry.Process("IncrementAsync", named_params).Result;
            Assert.AreEqual<int>(JsonSerializer.Deserialize<int>(result), 2);

            result = registry.Process("Print", unnamed_params).Result;
            Assert.AreEqual<string>(result, "");

            registry.Remove(calculator.Increment);
            Assert.IsFalse(registry.Contains("Increment"));
        }
    }
}
