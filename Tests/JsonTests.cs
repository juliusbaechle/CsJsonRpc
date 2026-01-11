using System.Text.Json;
using System.Text.Json.Nodes;

namespace Tests {
    [TestClass]
    public sealed class JsonTests {
        internal class Order {
            public Order(int id, string name) {
                Id = id;
                Name = name;
            }

            public int Id { get; set; }
            public string Name { get; set; } = "";
        }

        [TestMethod]
        public async Task ConvertObject_EqualsInput() {
            Order order = new Order(0, "M Coffee");
            var json = JsonSerializer.Serialize(order);
            var obj = JsonSerializer.Deserialize<JsonObject>(json);
            Assert.IsNotNull(obj);
            Assert.IsNull(obj["non_existent"]);
            Assert.AreEqual<int>(0, obj["Id"].Deserialize<int>());
            Console.WriteLine(obj);
        }

        [TestMethod]
        public async Task SerializeAndDeserializeArray_EqualsInput() {
            int[] arr = [1, 2, 3, 4];
            var json = JsonSerializer.Serialize(arr);
            var json_array = JsonSerializer.Deserialize<JsonArray>(json);

            Assert.AreEqual(arr.Length, json_array?.Count);
            for (int i = 0; i < arr.Length; i++)
                Assert.AreEqual(arr[i], json_array?[i].Deserialize<int>());
        }

        [TestMethod]
        public async Task DeserializeIntoWrongType_ThrowsJsonException() {
            Order order = new Order(0, "M Coffee");
            var json = JsonSerializer.Serialize(order);
            var doc = JsonDocument.Parse(json);
            Assert.Throws<JsonException>(() => doc.Deserialize<Array>());

            var obj = doc.Deserialize<JsonObject>();
            Assert.IsNotNull(obj);
        }

        [TestMethod]
        public async Task SerializeList() {
            List<int> sent_list = [1, 2, 3, 4];
            var node = JsonSerializer.SerializeToNode(sent_list);
            var recv_list = node.Deserialize<List<int>>();
            Assert.AreEqual(4, recv_list.Count);
        }

        [TestMethod]
        public async Task ParseInvalidJson_ThrowsJsonException() {
            Assert.Throws<JsonException>(() => JsonDocument.Parse("""(XXX-XXX)"""));
        }
    }
}
