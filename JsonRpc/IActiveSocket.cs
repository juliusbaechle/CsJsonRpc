namespace JsonRpc
{
    public interface IActiveSocket : IDisposable
    {
        public int Id { get; }

        public void Send(string a_msg);

        public event Action<string> ReceivedMsg;
        
        public bool Connected { get; }

        public event Action<bool> ConnectionChanged;
    }
}
