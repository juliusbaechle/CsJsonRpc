using JsonRpc;
using System.Net;

namespace Tests {

    [TestClass]
    public sealed class TcpSocketTests {
        [TestMethod]
        public async Task EchoMessage() {
            var endpoint = new IPEndPoint(IPAddress.Loopback, 1234);
            var server = new PassiveTcpSocket(endpoint);
            var client = new ActiveTcpSocket(endpoint);

            string rcv = "";
            TaskCompletionSource receivedTCS = new();
            client.ReceivedMsg += (s) => {
                rcv = s;
                receivedTCS.SetResult();
            };

            server.ClientConnected += (IActiveSocket socket) => {
                socket.ReceivedMsg += socket.Send;
                Assert.AreEqual(socket.ConnectionId, client.ConnectionId);
            };

            await client.ConnectAsync();
            client.Send("Hello World!");

            await receivedTCS.Task;
            Assert.AreEqual("Hello World!", rcv);
            Assert.IsTrue(client.Connected);

            client.Dispose();
            server.Dispose();
        }

        [TestMethod]
        public async Task Disconnects() {
            TaskCompletionSource disconnectTCS = new();
            var client = new ActiveTcpSocket(new IPEndPoint(IPAddress.Loopback, 1234));
            client.ConnectionChanged += (bool b) => { disconnectTCS.SetResult(); };

            IActiveSocket socket = null;
            TaskCompletionSource clientConnectedTCS = new();
            var server = new PassiveTcpSocket(new IPEndPoint(IPAddress.Loopback, 1234));
            server.ClientConnected += (s) => {
                socket = s;
                clientConnectedTCS.SetResult();
            };

            client.ConnectAsync();
            await clientConnectedTCS.Task;

            socket.Dispose();
            await disconnectTCS.Task;

            client.Dispose();
            server.Dispose();
        }
    }
}
