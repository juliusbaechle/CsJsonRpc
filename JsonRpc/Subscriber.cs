using System.Text.Json.Nodes;

namespace JsonRpc {
    public class Subscriber : IDisposable {
        public Subscriber(Client a_client, IActiveSocket a_socket) {
            m_client = a_client;
            m_socket = a_socket;
            m_socket.ConnectionChanged += OnConnectionChanged;
            m_methodRegistry = new();
            m_exceptionConverter = new();
            m_processor = new(m_methodRegistry, m_exceptionConverter);
            m_socket.ReceivedMsg += (s) => { m_socket.Send(m_processor.HandleRequest(s)); };
            m_subscriptions = [];
        }

        public void Dispose() {
            m_mutex.WaitOne();
            m_socket.Dispose();
            m_mutex.ReleaseMutex();
        }

        public Task SubscribeAsync(string a_subscription, Delegate a_delegate, List<string>? a_mapping = null) {
            m_methodRegistry.Add(a_subscription, a_delegate, a_mapping);
            return m_client.Request("Subscribe", new JsonObject { { "Subscription", a_subscription }, { "ClientId", m_socket.ConnectionId } });
        }

        public Task UnsubscribeAsync(string a_subscription) {
            if (!m_methodRegistry.Contains(a_subscription))
                throw new JsonRpcException(JsonRpcException.ErrorCode.subscription_not_found, "subscr. " + a_subscription + " does not exist");
            m_methodRegistry.Remove(a_subscription);
            return m_client.Request("Unsubscribe", new JsonObject { { "Subscription", a_subscription }, { "ClientId", m_socket.ConnectionId } });
        }

        private void OnConnectionChanged(bool a_connected) {
            if (!a_connected)
                return;

            foreach (var subscription in m_methodRegistry.Methods)
                m_client.Notify("Subscribe", new JsonObject { { "Subscription", subscription }, { "ClientId", m_socket.ConnectionId } });
        }

        public bool IsSubscribed(string a_subscription) {
            return m_methodRegistry.Contains(a_subscription);
        }

        private Mutex m_mutex = new();
        private Client m_client;
        private IActiveSocket m_socket;
        private MethodRegistry m_methodRegistry;
        private ExceptionConverter m_exceptionConverter;
        private RequestProcessor m_processor;
        private List<string> m_subscriptions;
    }
}
