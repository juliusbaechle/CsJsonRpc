using JsonRpc;
using System.Net;

namespace Tests
{
    [TestClass]
    public sealed class SubscriptionTests
    {
        [TestMethod]
        public async Task NormalUseCase()
        {
            IPEndPoint serverEndpoint = new(IPAddress.Loopback, 1000);
            IPEndPoint publisherEndpoint = new(IPAddress.Loopback, 1001);

            PassiveTcpSocket serverSocket = new(serverEndpoint);
            ActiveTcpSocket clientSocket = new(serverEndpoint);

            PassiveTcpSocket publisherSocket = new(publisherEndpoint);
            ActiveTcpSocket subscriberSocket = new(publisherEndpoint);

            Server server = new(serverSocket, new(), new());
            Client client = new(clientSocket, new());

            Publisher publisher = new(server, publisherSocket);
            Subscriber subscriber = new(client, subscriberSocket);

            await Task.WhenAll(clientSocket.ConnectAsync(), subscriberSocket.ConnectAsync());

            publisher.Add("Subscription");
            Assert.Throws<JsonRpcException>(() => { publisher.Add("Subscription"); });
            Assert.Throws<JsonRpcException>(() => { publisher.Remove("NotContained"); });
            Assert.IsTrue(publisher.Contains("Subscription"));

            TaskCompletionSource subscriberCalled = new();
            await subscriber.SubscribeAsync("Subscription", () => { subscriberCalled.SetResult(); });

            Assert.IsTrue(publisher.IsActive("Subscription"));

            publisher.Publish("Subscription");
            await subscriberCalled.Task;

            await subscriber.UnsubscribeAsync("Subscription");
            
            publisher.Remove("Subscription");
            Assert.IsFalse(publisher.Contains("Subscription"));

            publisher.Dispose();
            server.Dispose();
            subscriber.Dispose();
            client.Dispose();
        }
    }
}
