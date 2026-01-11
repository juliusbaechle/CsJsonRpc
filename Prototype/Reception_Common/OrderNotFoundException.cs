using System.Text.Json.Nodes;

namespace Reception_Common {
    [Serializable]
    public class OrderNotFoundException : Exception {
        public OrderNotFoundException() { }

        public OrderNotFoundException(string message)
            : base(message) { }

        public OrderNotFoundException(string message, Exception innerException)
            : base(message, innerException) { }

        public static implicit operator JsonObject(OrderNotFoundException e) {
            return new JsonObject { { "message", e.Message } };
        }

        public static implicit operator OrderNotFoundException(JsonObject o) {
            var msg = o["message"]?.GetValue<string>();
            return new OrderNotFoundException(msg ?? "");
        }
    }
}
