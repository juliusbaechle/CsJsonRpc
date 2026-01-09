using JsonRpc;
using System.Net;

namespace Tests
{

    [TestClass]
    public sealed class TcpSocketTests
    {
        [TestMethod]
        public async Task EchoMessage()
        {
            var server = new PassiveTcpSocket(new IPEndPoint(IPAddress.Loopback, 1234));
            var client = new ActiveTcpSocket(new IPEndPoint(IPAddress.Loopback, 1234));

            string rcv = "";
            client.ReceivedMsg += (s) => rcv = s;

            server.ClientConnected += (IActiveSocket socket) =>
            {
                socket.ReceivedMsg += socket.Send;
                Assert.AreEqual(socket.ConnectionId, client.ConnectionId);
            };

            client.ConnectAsync().Wait();

            client.Send("Hello World!");

            while (rcv == "") 
                Thread.Sleep(5);
            Assert.AreEqual("Hello World!", rcv);
            Assert.IsTrue(client.Connected);

            client.Dispose();
            server.Dispose();
        }

        [TestMethod]
        public async Task Disconnects()
        {
            bool connected = true;
            var client = new ActiveTcpSocket(new IPEndPoint(IPAddress.Loopback, 1234));
            client.ConnectionChanged += (bool b) => { connected = b; };

            IActiveSocket socket = null;
            var server = new PassiveTcpSocket(new IPEndPoint(IPAddress.Loopback, 1234));
            server.ClientConnected += (s) => socket = s;

            client.ConnectAsync();

            while (socket == null)
                Thread.Sleep(5);
            socket.Dispose();

            while (connected)
                Thread.Sleep(5);

            client.Dispose();
            server.Dispose();
        }
    }
}
