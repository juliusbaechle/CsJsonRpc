using System;
using System.Collections.Generic;
using System.Text;

namespace Tests.Mocks
{
    internal class InMemoryConnector : JsonRpc.IClientConnector
    {
        public InMemoryConnector(JsonRpc.Server a_server)
        {
            m_server = a_server;
        }

        public string Send(string request) { 
            return m_server.HandleRequest(request); 
        }

        JsonRpc.Server m_server;
    }
}
