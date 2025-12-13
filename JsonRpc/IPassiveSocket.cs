namespace JsonRpc
{
    public interface IPassiveSocket : IDisposable
    {
        delegate void ConnectHandler(IActiveSocket client);
        event ConnectHandler ClientConnected;
        event LogHandler Log;
    }
}
