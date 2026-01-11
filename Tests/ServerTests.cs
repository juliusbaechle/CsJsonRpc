using JsonRpc;
using Tests.Mocks;

namespace Tests
{
    [TestClass]
    public sealed class ServerTests
    {
        [TestMethod]
        public async Task Test()
        {
            PassiveMockSocket passiveSocket = new();
            MethodRegistry methodRegistry = new();
            Server server = new(passiveSocket, methodRegistry, new());

            ActiveMockSocket activeSocket = new(passiveSocket);
            Client client = new(activeSocket, new());

            await client.ConnectAsync();

            activeSocket.Disconnect();


            passiveSocket.Dispose();
            activeSocket.Dispose();
        }
    }
}
