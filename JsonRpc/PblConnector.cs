namespace JsonRpc {
    public class PblConnector : IDisposable {
        public PblConnector(IPassiveSocket a_socket) {
            m_socket = a_socket;
            m_socket.ClientConnected += OnClientConnected;
        }

        public void Dispose() {
            using (new ReadContext(m_lock)) {
                foreach (var c in m_clients.Values)
                    c.Dispose();
                m_socket.Dispose();
            }
        }

        public void Send(long a_id, string a_msg) {
            using (new ReadContext(m_lock)) {
                IActiveSocket? socket = null;
                if (m_clients.TryGetValue(a_id, out socket))
                    socket.Send(a_msg);
            }
        }

        public void SendAll(string a_msg) {
            using (new ReadContext(m_lock)) {
                foreach (var c in m_clients.Values)
                    c.Send(a_msg);
            }
        }

        public bool IsConnected(long a_id) {
            using (new ReadContext(m_lock))
                return m_clients.ContainsKey(a_id);
        }

        public List<long> AllConnected() {
            using (new ReadContext(m_lock))
                return m_clients.Keys.ToList();
        }

        public event Action<long, string> ReceivedMsg = (i, s) => { };
        public event Action<long, bool> ConnectionChanged = (i, b) => { };

        private void OnClientConnected(IActiveSocket a_socket) {
            using (new WriteContext(m_lock))
                m_clients[a_socket.ConnectionId] = a_socket;
            a_socket.ReceivedMsg += (s) => { ReceivedMsg(a_socket.ConnectionId, s); };
            a_socket.ConnectionChanged += (b) => { OnConnectionChanged(a_socket.ConnectionId, b); };
        }

        private void OnConnectionChanged(long a_id, bool a_connected) {
            if (!a_connected && m_clients.ContainsKey(a_id)) {
                using (new WriteContext(m_lock)) {
                    m_clients[a_id].Dispose();
                    m_clients.Remove(a_id);
                }
                ConnectionChanged(a_id, a_connected);
            }
        }

        private ReaderWriterLock m_lock = new();
        private Dictionary<long, IActiveSocket> m_clients = [];
        IPassiveSocket m_socket;
    }
}
