using JsonRpc;

namespace Tests.Mocks
{
    public class ActiveMockSocket : IActiveSocket
    {
        public ActiveMockSocket(PassiveMockSocket a_passiveSocket)
        {
            m_connectionId = new Random().NextInt64();
            m_peer = new(this);
            m_passiveSocket = a_passiveSocket;
        }

        private ActiveMockSocket(ActiveMockSocket a_peer) 
        {
            m_peer = a_peer;
            m_connectionId = a_peer.ConnectionId;
            m_passiveSocket = a_peer.m_passiveSocket;
            m_connected = true;
        }

        public void Dispose()
        {
            m_peer.SetConnected(false);
        }

        public long ConnectionId { get { return m_connectionId; } }

        public bool Connected { get { return m_connected; } }

        public event Action<string> ReceivedMsg = (s) => { };
        public event Action<bool> ConnectionChanged = (b) => { };

        public Task ConnectAsync()
        {
            TaskCompletionSource tcs = new();
            m_passiveSocket.EmitClientConnected(m_peer);
            SetConnected(true);
            tcs.SetResult();
            return tcs.Task;
        }

        public void Disconnect()
        {
            SetConnected(false);
            m_peer.SetConnected(false);
        }

        public void Send(string a_msg)
        {
            if (m_connected)
                m_peer.ReceivedMsg(a_msg);
            else
                throw new Exception("Socket was disconnected");
        }

        private void SetConnected(bool a_connected)
        {
            if (m_connected == a_connected)
                return;
            m_connected = a_connected;
            ConnectionChanged(a_connected);
        }

        private bool m_connected = false;
        private long m_connectionId = 0;
        private PassiveMockSocket m_passiveSocket;
        private ActiveMockSocket m_peer;
    }
}
