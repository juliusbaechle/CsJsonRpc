using JsonRpc;
using Reception_Common;
using System.Net;
using System.Text.Json.Nodes;

namespace Server {
    public class ReceptionServer : IDisposable {
        public ReceptionServer(Reception a_reception) {
            m_reception = a_reception;
            m_exceptionConverter = new();
            ReceptionExceptions.RegisterReceptionExceptions(m_exceptionConverter);
            m_serverSocket = new PassiveTcpSocket(new IPEndPoint(IPAddress.Loopback, WellKnownPorts.SrvPort));
            m_server = new JsonRpc.Server(m_serverSocket, SetupMethodRegistry(), m_exceptionConverter);
            m_pblSocket = new PassiveTcpSocket(new IPEndPoint(IPAddress.Loopback, WellKnownPorts.PblPort));
            m_publisher = new JsonRpc.Publisher(m_server, m_pblSocket);
            SetupSubscriptions();
        }

        public void Dispose() {
            m_server.Dispose();
            m_publisher.Dispose();
        }

        private MethodRegistry SetupMethodRegistry() {
            MethodRegistry registry = new();
            registry.Add("AppendOrder", m_reception.AppendOrder, ["Order"]);
            registry.Add("StartOrder", m_reception.StartOrder, ["OrderId"]);
            registry.Add("GetOrder", m_reception.GetOrder, ["OrderId"]);
            return registry;
        }

        private void SetupSubscriptions() {
            m_publisher.Add("OrderStateChanged");
            m_reception.OrderStateChanged += (int a_id, Order.EState a_state) => {
                m_publisher.Publish("OrderStateChanged", new JsonObject { { "OrderId", a_id }, { "OrderState", a_state.ToString() } });
            };
        }

        private JsonRpc.ExceptionConverter m_exceptionConverter;
        private JsonRpc.IPassiveSocket m_serverSocket;
        private JsonRpc.Server m_server;
        private JsonRpc.IPassiveSocket m_pblSocket;
        private JsonRpc.Publisher m_publisher;
        private Reception m_reception;
    }
}