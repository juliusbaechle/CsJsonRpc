namespace JsonRpc {
    public class Server {
        public Server(IPassiveSocket a_passiveSocket, MethodRegistry a_methodRegistry, ExceptionConverter a_exceptionConverter) {
            m_passiveSocket = a_passiveSocket;
            m_methodRegistry = a_methodRegistry;
            m_exceptionConverter = a_exceptionConverter;
            m_passiveSocket.ClientConnected += AddClient;
        }

        public void Dispose() {
            using (m_lock.EnterScope()) {
                m_requestProcessors.Clear();
                foreach (var s in m_activeSockets.Values)
                    s.Dispose();
                m_activeSockets.Clear();
                m_passiveSocket.Dispose();
            }
        }

        public MethodRegistry MethodRegistry { get { return m_methodRegistry; } }

        private void AddClient(IActiveSocket a_socket) {
            using (m_lock.EnterScope()) {
                m_activeSockets.Add(a_socket.ConnectionId, a_socket);
                var requestProcessor = new RequestProcessor(m_methodRegistry, m_exceptionConverter);
                a_socket.ReceivedMsg += (s) => { s = requestProcessor.HandleRequest(s); if (s != "null") a_socket.Send(s); };
                a_socket.ConnectionChanged += (bool c) => { if (!c) OnClientDisconnected(a_socket.ConnectionId); };
                m_requestProcessors.Add(a_socket.ConnectionId, requestProcessor);
            }
        }

        private void OnClientDisconnected(long a_id) {
            using (m_lock.EnterScope()) {
                m_activeSockets.Remove(a_id);
                m_requestProcessors.Remove(a_id);
            }
        }

        private Lock m_lock = new();
        private IPassiveSocket m_passiveSocket;
        private ExceptionConverter m_exceptionConverter;
        private MethodRegistry m_methodRegistry;
        private Dictionary<long, IActiveSocket> m_activeSockets = [];
        private Dictionary<long, RequestProcessor> m_requestProcessors = [];
    }
}
