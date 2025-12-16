using JsonRpc;
using System.Net;
using System.Text.Json;

namespace Tests
{
    [TestClass]
    public sealed class TransportTests
    {
        static void Log(String msg, LogSeverity severity)
        {
            Console.Out.WriteLine(severity.ToString().ToUpper() + ": " + msg);
        }

        [TestMethod]
        public void TestTransport()
        {
            bool pinged = false;

            var endPoint = new IPEndPoint(new IPAddress([127, 0, 0, 1]), 1234);
            IActiveSocket client = new TcpActiveSocket(endPoint, "client");
            client.Log += Log;
            client.ReceivedMsg += client.Send; // Echo

            IPassiveSocket server = new TcpPassiveSocket(1234, "server");
            List<IActiveSocket> clients = [];
            server.Log += Log;
            server.ClientConnected += (client) =>
            {
                client.Log += Log;
                client.ReceivedMsg += (msg) => { pinged = true; };
                client.Send("Heartbeat");
                clients.Add(client);
            };

            while (!pinged);

            client.Dispose();
            server.Dispose();
            clients.ForEach(c => c.Dispose());
            Console.Out.WriteLine("END");
        }
    }
}
