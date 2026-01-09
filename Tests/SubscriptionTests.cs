using JsonRpc;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Tests
{
    [TestClass]
    public sealed class SubscriptionTests
    {
        [TestMethod]
        public async Task CanConnectMultipleSubscribers()
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

            TaskCompletionSource subscriberCalled = new();
            await subscriber.SubscribeAsync("Subscription", () => { subscriberCalled.SetResult(); });

            publisher.Publish("Subscription");
            await subscriberCalled.Task;
        }
    }
}
