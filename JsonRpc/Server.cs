using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JsonRpc
{
    public class Server
    {
        public Server(IPassiveSocket a_passiveSocket, MethodRegistry a_methodRegistry, ExceptionConverter a_exceptionConverter)
        {
            m_passiveSocket = a_passiveSocket;
            m_methodRegistry = a_methodRegistry;
            m_exceptionConverter = a_exceptionConverter;
            m_passiveSocket.ClientConnected += AddClient;
        }

        public void Dispose()
        {
            m_mutex.WaitOne();
            m_requestProcessors.Clear();
            m_activeSockets.Clear();
            m_mutex.ReleaseMutex();
        }

        private void AddClient(IActiveSocket a_socket)
        {
            m_mutex.WaitOne();
            m_activeSockets.Add(a_socket.Id, a_socket);
            var requestProcessor = new RequestProcessor(m_methodRegistry, m_exceptionConverter);
            a_socket.ReceivedMsg += (s) => { a_socket.Send(requestProcessor.HandleRequest(s)); };
            a_socket.ConnectionChanged += (bool c) => { if (!c) OnClientDisconnected(a_socket.Id); };
            m_requestProcessors.Add(a_socket.Id, requestProcessor);
            m_mutex.ReleaseMutex();
        }

        private void OnClientDisconnected(int a_id)
        {
            m_mutex.WaitOne();
            m_activeSockets.Remove(a_id);
            m_requestProcessors.Remove(a_id);
            m_mutex.ReleaseMutex();
        }

        private Mutex m_mutex = new();
        private IPassiveSocket m_passiveSocket;
        private ExceptionConverter m_exceptionConverter;
        private MethodRegistry m_methodRegistry;
        private Dictionary<int, IActiveSocket> m_activeSockets = [];
        private Dictionary<int, RequestProcessor> m_requestProcessors = [];
    }
}
