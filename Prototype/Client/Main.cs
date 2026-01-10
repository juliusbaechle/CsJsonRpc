using Client;

var client = new ReceptionClient();
var backgroundAction = new MockAction(client);
client.OrderStateChanged += (id, s) => { Console.WriteLine("INFO: State of order " + id + " changed to " + s.ToString()); };

await client.ConnectAsync();
await client.SubscribeAsync();

await backgroundAction.SimulateAction();