using JsonRpc;

namespace Tests.Mocks
{
    public class PassiveMockSocket : IPassiveSocket
    {
        public event Action<IActiveSocket> ClientConnected = (s) => { };

        public void EmitClientConnected(ActiveMockSocket a_socket) { ClientConnected(a_socket); }

        public void Dispose() { }
    }
}
