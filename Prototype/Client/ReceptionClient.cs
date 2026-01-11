using JsonRpc;
using Reception_Common;
using System.Net;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using static Reception_Common.Order;

namespace Client {
    public class ReceptionClient : IReceptionClient {
        public ReceptionClient() {
            m_exceptionConverter = new();
            ReceptionExceptions.RegisterReceptionExceptions(m_exceptionConverter);
            m_clientSocket = new ActiveTcpSocket(new IPEndPoint(IPAddress.Loopback, WellKnownPorts.SrvPort));
            m_client = new(m_clientSocket, m_exceptionConverter);
            m_subscriberSocket = new ActiveTcpSocket(new IPEndPoint(IPAddress.Loopback, WellKnownPorts.PblPort));
            m_subscriber = new(m_client, m_subscriberSocket);
        }

        public Task SubscribeAsync() {
            return m_subscriber.SubscribeAsync("OrderStateChanged", OnOrderStateChanged, ["OrderId", "OrderState"]);
        }

        public Task ConnectAsync() {
            return Task.WhenAll([m_clientSocket.ConnectAsync(), m_subscriberSocket.ConnectAsync()]);
        }

        public void Dispose() {
            m_clientSocket.Dispose();
            m_client.Dispose();
        }

        public Task<int> AppendOrder(Order a_order) {
            return m_client.Request<int>("AppendOrder", new JsonArray([JsonSerializer.SerializeToNode(a_order)]));
        }

        public void StartOrder(int a_id) {
            m_client.Notify("StartOrder", new JsonObject { { "OrderId", a_id } });
        }

        public Task<Order> GetOrder(int a_id) {
            return m_client.Request<Order>("GetOrder", new JsonObject { { "OrderId", a_id } });
        }

        private void OnOrderStateChanged(int a_id, string a_state) {
            var state = (EState)Enum.Parse(typeof(EState), a_state, true);
            OrderStateChanged(a_id, state);
        }

        public event Action<int, Order.EState> OrderStateChanged = (i, s) => { };

        private IActiveSocket m_clientSocket;
        private JsonRpc.Client m_client;
        private JsonRpc.ExceptionConverter m_exceptionConverter;
        private IActiveSocket m_subscriberSocket;
        private JsonRpc.Subscriber m_subscriber;
    }
}