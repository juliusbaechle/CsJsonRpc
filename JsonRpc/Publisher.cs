using System.Text.Json;
using System.Text.Json.Nodes;

namespace JsonRpc
{
    public class Publisher
    {
        public Publisher(Server a_server, IPassiveSocket a_socket)
        {
            m_server = a_server;
            m_connector = new PblConnector(a_socket);
            m_connector.ConnectionChanged += OnConnectionChanged;
            a_server.Add("Subscribe", Subscribe, ["Subscription", "ClientId"]);
            a_server.Add("Unsubscribe", Unsubscribe, ["Subscription", "ClientId"]);
        }

        public void Dispose()
        {
            m_server.Remove("Subscribe");
            m_server.Remove("Unsubscribe");
            m_connector.Dispose();
        }

        public void Add(string a_subscription)
        {
            if (m_subscriptions.ContainsKey(a_subscription))
                throw new JsonRpcException(JsonRpcException.ErrorCode.internal_error, "subscription " + a_subscription + " already exists");
            m_subscriptions.Add(a_subscription, []);
        }

        public void Remove(string a_subscription)
        {
            CheckRegistered(a_subscription);
            m_subscriptions.Remove(a_subscription);
        }

        public bool Contains(string a_subscription)
        {
            return m_subscriptions.ContainsKey(a_subscription);
        }

        public void Publish(string a_subscription, JsonNode? a_params = null)
        {
            m_mutex.WaitOne();
            CheckRegistered(a_subscription);
            string publication = JsonSerializer.Serialize(JsonBuilders.Notify(a_subscription, a_params));

            foreach (var id in m_subscriptions[a_subscription])
            {
                try
                {
                    m_connector.Send(id, publication);
                }
                catch (Exception ex)
                {
                    Logging.LogWarning("Failed to send subscr. " + a_subscription + " to client " + id);
                }
            }
            m_mutex.ReleaseMutex();
        }

        private void Subscribe(string a_subscription, long a_clientId)
        {
            m_mutex.WaitOne();
            CheckRegistered(a_subscription);
            if (!m_subscriptions[a_subscription].Contains(a_clientId))
                m_subscriptions[a_subscription].Add(a_clientId);
            m_mutex.ReleaseMutex();
        }

        private void Unsubscribe(string a_subscription, long a_clientId)
        {
            m_mutex.WaitOne();
            CheckRegistered(a_subscription);
            m_subscriptions[a_subscription].Remove(a_clientId);
            m_mutex.ReleaseMutex();
        }

        private void OnConnectionChanged(long a_clientId, bool a_connected)
        {
            if (a_connected)
                return;

            m_mutex.WaitOne();
            foreach (var subscription in m_subscriptions.Keys)
                m_subscriptions[subscription].Remove(a_clientId);
            m_mutex.ReleaseMutex();
        }

        public bool IsActive(string a_subscription)
        {
            m_mutex.WaitOne();
            CheckRegistered(a_subscription);
            bool result = m_subscriptions[a_subscription].Count > 0;
            m_mutex.ReleaseMutex();
            return result;
        }

        private void CheckRegistered(string a_subscription)
        {
            if (!m_subscriptions.ContainsKey(a_subscription))
                throw new JsonRpcException(JsonRpcException.ErrorCode.subscription_not_found, "subscription " + a_subscription + " does not exist");
        }

        private Mutex m_mutex = new();
        private Server m_server;
        private Dictionary<string, List<long>> m_subscriptions = [];
        private PblConnector m_connector;
    }
}
