using JsonRpc;

namespace Tests.Mocks
{
    public class ClientConnectorMock : IClientConnector
    {
        public ClientConnectorMock(string a_response = "") { Response = a_response; }

        public string Send(string a_message) { CapturedRequest = a_message; return Response; }

        public string Response { get; set; } = "";
        public string CapturedRequest { get; set; } = "";
    }
}
