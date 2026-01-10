using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using static Reception_Common.Order;

namespace Reception_Common
{
    public class Order
    {
        public enum EState { Created, InProgress, Finished }

        public int Id { get; set; }
        public string Name { get; set; } = "";
        public EState State { get; set; } = Order.EState.Created;
    }
}
