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
    public sealed class JsonTests
    {
        internal class Order
        {
            public Order(int id, string name)
            {
                Id = id;
                Name = name;
            }

            public int Id { get; set; }
            public string Name { get; set; } = "";
        }

        [TestMethod]
        public async Task ParseObject()
        {
            Order order = new Order(0, "M Coffee");
            var json = JsonSerializer.Serialize(order);
            var obj = JsonDocument.Parse(json).Deserialize<JsonObject>();
            Assert.IsNotNull(obj);
            var id = obj["Id"];
            Assert.IsNull(obj["non_existent"]);
            Console.WriteLine(obj);
        }

        [TestMethod]
        public async Task ParseArray()
        {
            var json = JsonSerializer.Serialize<JsonArray>([1, 2, 3, 4]);
            var obj = JsonDocument.Parse(json).Deserialize<JsonArray>();
            Console.WriteLine(obj);
        }

        [TestMethod]
        public async Task ParseArrayFailed()
        {
            Order order = new Order(0, "M Coffee");
            var json = JsonSerializer.Serialize(order);
            Assert.Throws<JsonException>(() => JsonDocument.Parse(json).Deserialize<Array>());
        }
    }
}
