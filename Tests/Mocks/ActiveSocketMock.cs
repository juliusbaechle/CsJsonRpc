using JsonRpc;

namespace Tests.Mocks
{
    public class ActiveSocketMock : IActiveSocket
    {
        public ActiveSocketMock(string a_response = "") { Response = a_response; }

        public event Action<string> ReceivedMsg = (s) => { };
        public event Action<bool> ConnectionChanged = (b) => { };

        public void Send(string a_msg)
        {
            CapturedRequest = a_msg;
            ReceivedMsg(Response);            
        }

        public void Dispose()
        {            
        }

        public Task ConnectAsync()
        {
            throw new NotImplementedException();
        }

        public string Response { get; set; } = "";
        public string CapturedRequest { get; set; } = "";

        public long ConnectionId { get; } = 0;

        public bool Connected { get; } = true;
    }
}
