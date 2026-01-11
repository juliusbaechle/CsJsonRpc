using System.Net;
using System.Net.Sockets;

namespace JsonRpc {
    public class PassiveTcpSocket : IPassiveSocket {
        public PassiveTcpSocket(EndPoint a_endPoint) {
            m_socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            m_socket.Bind(a_endPoint);
            m_socket.Listen();
            m_thread = new Thread(new ThreadStart(Listen));
            m_thread.Start();
        }

        public void Dispose() {
            m_terminate.Cancel();
            m_thread.Join();
            m_socket.Dispose();
        }

        private async void Listen() {
            try {
                while (!m_terminate.IsCancellationRequested) {
                    var socket = await m_socket.AcceptAsync(m_terminate.Token);
                    if (socket != null) {
                        var activeTcpSocket = new ActiveTcpSocket(socket);
                        ClientConnected(activeTcpSocket);
                        activeTcpSocket.StartListening();
                    }
                }
            } catch (Exception ex) {
                Logging.LogError(ex.ToString());
            }
        }

        public event Action<IActiveSocket> ClientConnected = (IActiveSocket s) => { };

        private Thread m_thread;
        private Socket m_socket;
        private CancellationTokenSource m_terminate = new();
    }
}
