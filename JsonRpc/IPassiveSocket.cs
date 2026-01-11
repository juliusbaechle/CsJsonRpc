namespace JsonRpc {
    public interface IPassiveSocket : IDisposable {
        public event Action<IActiveSocket> ClientConnected;
    }
}