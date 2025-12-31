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
            string rcv = "";
            var client = new ActiveTcpSocket(new IPEndPoint(IPAddress.Loopback, 1234));
            client.ReceivedMsg += (string s) => { rcv = s; };
            client.ConnectionChanged += (bool b) => { client.Send("Hello World!"); };

            var server = new PassiveTcpSocket(new IPEndPoint(IPAddress.Loopback, 1234));
            server.ClientConnected += (IActiveSocket socket) =>
            {
                socket.ReceivedMsg += socket.Send;
                Assert.AreNotEqual(socket.Id, client.Id);
            };

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

            var server = new PassiveTcpSocket(new IPEndPoint(IPAddress.Loopback, 1234));
            server.ClientConnected += (IActiveSocket socket) =>
            {
                socket.Dispose();
            };

            while (connected)
                Thread.Sleep(5);

            client.Dispose();
            server.Dispose();
        }
    }
}
