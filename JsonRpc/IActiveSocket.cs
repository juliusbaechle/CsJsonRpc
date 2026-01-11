namespace JsonRpc {
    public interface IActiveSocket : IDisposable {
        public long ConnectionId { get; }

        public void Send(string a_msg);

        public event Action<string> ReceivedMsg;

        public Task ConnectAsync();

        public bool Connected { get; }

        public event Action<bool> ConnectionChanged;
    }
}
