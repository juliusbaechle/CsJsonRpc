using JsonRpc;
using System.Net;

namespace Tests
{
    [TestClass]
    public sealed class PblConnectorTests
    {
        [TestMethod]
        public async Task CombinesMultipleClients()
        {
            var endpoint = new IPEndPoint(IPAddress.Loopback, 1234);
            var passiveSocket = new PassiveTcpSocket(endpoint);
            var connector = new PblConnector(passiveSocket);

            var client = new ActiveTcpSocket(endpoint);

            await client.ConnectAsync();
            Assert.IsTrue(connector.IsConnected(client.ConnectionId));
            Assert.HasCount(1, connector.AllConnected());

            TaskCompletionSource<string> receivedTCS1 = new();
            client.ReceivedMsg += receivedTCS1.SetResult;
            connector.Send(client.ConnectionId, "Msg1");
            Assert.AreEqual("Msg1", await receivedTCS1.Task);

            TaskCompletionSource<string> receivedTCS2 = new();
            client.ReceivedMsg -= receivedTCS1.SetResult;
            client.ReceivedMsg += receivedTCS2.SetResult;
            connector.SendAll("Msg2");
            Assert.AreEqual("Msg2", await receivedTCS2.Task);

            long id = 0;
            TaskCompletionSource<string> receivedTCS3 = new();
            connector.ReceivedMsg += (l, s) => { id = l; receivedTCS3.SetResult(s); };
            client.Send("Msg3");
            Assert.AreEqual("Msg3", await receivedTCS3.Task);
            Assert.AreEqual(client.ConnectionId, id);

            TaskCompletionSource disconnectTCS = new();
            connector.ConnectionChanged += (l, b) => { disconnectTCS.SetResult(); };
            client.Dispose();
            await disconnectTCS.Task;
            Assert.HasCount(0, connector.AllConnected());

            connector.Dispose();
        }
    }
}
