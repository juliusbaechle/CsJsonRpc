using System;
using System.Collections.Generic;
using System.Text;

namespace JsonRpc
{
    public class PblConnector : IDisposable
    {
        public PblConnector(IPassiveSocket a_socket)
        {
            m_socket = a_socket;
            m_socket.ClientConnected += OnClientConnected;
        }

        public void Dispose()
        {
            m_lock.AcquireWriterLock(0);
            foreach (var c in m_clients.Values)
                c.Dispose();
            m_lock.ReleaseWriterLock();
        }

        public void Send(long a_id, string a_msg)
        {
            m_lock.AcquireReaderLock(0);
            IActiveSocket? socket = null;
            if (m_clients.TryGetValue(a_id, out socket))
                socket.Send(a_msg);
            m_lock.ReleaseReaderLock();
        }

        public void SendAll(string a_msg)
        {
            m_lock.AcquireReaderLock(0);
            foreach (var c in m_clients.Values)
                c.Send(a_msg);
            m_lock.ReleaseReaderLock();
        }

        public bool IsConnected(long a_id)
        {
            bool result = false;
            m_lock.AcquireReaderLock(0);
            result = m_clients.ContainsKey(a_id);
            m_lock.ReleaseReaderLock();
            return result;
        }

        public List<long> AllConnected()
        {
            List<long> result = [];
            m_lock.AcquireReaderLock(0);
            result = m_clients.Keys.ToList<long>();
            m_lock.ReleaseReaderLock();
            return result;
        }
        
        public event Action<long, string> ReceivedMsg = (i, s) => { };
        public event Action<long, bool> ConnectionChanged = (i, b) => { };

        private void OnClientConnected(IActiveSocket a_socket)
        {
            m_lock.AcquireWriterLock(0);
            m_clients[a_socket.ConnectionId] = a_socket;
            m_lock.ReleaseWriterLock();
            a_socket.ReceivedMsg += (s) => { ReceivedMsg(a_socket.ConnectionId, s); };
            a_socket.ConnectionChanged += (b) => { OnConnectionChanged(a_socket.ConnectionId, b); };
        }

        private void OnConnectionChanged(long a_id, bool a_connected)
        {
            if (a_connected || !m_clients.ContainsKey(a_id))
                return;
            m_lock.AcquireWriterLock(0);
            m_clients[a_id].Dispose();
            m_clients.Remove(a_id);
            m_lock.ReleaseWriterLock();
            ConnectionChanged(a_id, a_connected);
        }

        private ReaderWriterLock m_lock = new();
        private Dictionary<long, IActiveSocket> m_clients = [];
        IPassiveSocket m_socket;
    }
}
