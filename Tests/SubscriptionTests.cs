using JsonRpc;
using System.Net;
using Tests.Mocks;

namespace Tests {
    [TestClass]
    public sealed class SubscriptionTests {
        [TestMethod]
        public async Task NormalUseCase() {
            PassiveMockSocket serverSocket = new();
            ActiveMockSocket clientSocket = new(serverSocket);

            PassiveMockSocket publisherSocket = new();
            ActiveMockSocket subscriberSocket = new(publisherSocket);

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

            subscriberSocket.Disconnect();
            Assert.IsFalse(publisher.IsActive("Subscription"));
            await subscriberSocket.ConnectAsync();
            Assert.IsTrue(publisher.IsActive("Subscription"));
            Assert.IsTrue(subscriber.IsSubscribed("Subscription"));

            await subscriber.UnsubscribeAsync("Subscription");
            Assert.IsFalse(publisher.IsActive("Subscription"));
            await Assert.ThrowsAsync<JsonRpcException>(async () => await subscriber.UnsubscribeAsync("Subscription"));

            publisher.Remove("Subscription");
            Assert.IsFalse(publisher.Contains("Subscription"));

            publisher.Dispose();
            server.Dispose();
            subscriber.Dispose();
            client.Dispose();
        }
    }
}
