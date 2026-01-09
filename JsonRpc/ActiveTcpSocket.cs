using System.Net;
using System.Net.Sockets;
using System.Text;

namespace JsonRpc
{
    public class ActiveTcpSocket : IActiveSocket
    {
        public ActiveTcpSocket(EndPoint a_endPoint)
        {
            m_socket = new(SocketType.Stream, ProtocolType.Tcp);
            m_socket.Blocking = true;
            m_connected = false;
            m_endPoint = a_endPoint;
            m_thread = new Thread(new ThreadStart(Run));
        }

        internal ActiveTcpSocket(Socket a_socket)
        {
            m_socket = a_socket;
            m_socket.Blocking = true;
            m_connected = true;
            m_thread = new Thread(new ThreadStart(Run));
        }

        internal void StartListening()
        {
            m_thread.Start();
        }

        public void Dispose()
        {
            m_terminate.Cancel();
            m_thread?.Join();
            m_socket.Shutdown(SocketShutdown.Both);
            m_socket.Dispose();
        }

        private void Run()
        {
            try
            {
                while (!m_terminate.IsCancellationRequested)
                {
                    if (m_connected)
                    {
                        ConnectedState();
                    }
                    else
                    {
                        NotConnectedState();
                    }
                }
            }
            catch (Exception) { }
        }

        private void NotConnectedState()
        {
            m_socket.ConnectAsync(m_endPoint, m_terminate.Token).AsTask().Wait();
            SetConnected(m_socket.Connected);
        }

        private void ConnectedState()
        {
            string buffer = "";
            while (m_connected && !m_terminate.IsCancellationRequested)
            {
                var arr = new byte[4096];
                var arr_seg = new ArraySegment<byte>(arr);
                
                var bytes_rec = m_socket.ReceiveAsync(arr_seg, m_terminate.Token).AsTask().Result;
                if (bytes_rec == 0)
                {
                    SetConnected(false);
                    return;
                }

                buffer = buffer + Encoding.UTF8.GetString(arr, 0, bytes_rec);
                if (buffer.Last() == 3)
                {
                    ReceivedMsg(buffer.Substring(0, buffer.Length - 1));
                    buffer = "";
                }
            }
        }

        private void SetConnected(bool connected)
        {
            if (m_connected != connected)
                ConnectionChanged(connected);
            if (connected)
                m_connectSource.SetResult();
            else
                m_connectSource = new();
            m_connected = connected;
        }

        public long ConnectionId { get { return (m_socket.RemoteEndPoint.GetHashCode() << 32) ^ m_socket.LocalEndPoint.GetHashCode(); } }

        public bool Connected { get { return m_connected; } }

        public event Action<string> ReceivedMsg = (string s) => { };

        public event Action<bool> ConnectionChanged = (bool b) => { };

        public void Send(string a_msg)
        {
            var encoded = Encoding.UTF8.GetBytes(a_msg + (char)3);
            var buffer = new ArraySegment<byte>(encoded, 0, encoded.Length);
            m_socket.Send(buffer);
        }

        public Task ConnectAsync()
        {
            StartListening();
            return m_connectSource.Task;
        }

        private volatile bool m_connected;
        private TaskCompletionSource m_connectSource = new();
        private readonly EndPoint m_endPoint;
        Socket m_socket;
        CancellationTokenSource m_terminate = new();
        Thread m_thread;
    }
}
