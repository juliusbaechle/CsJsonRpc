using Reception_Common;

namespace Client
{
    internal class MockAction
    {
        public MockAction(ReceptionClient a_client)
        {
            m_client = a_client;
            m_client.OrderStateChanged += OnOrderStateChanged;
        }

        public async Task SimulateAction()
        {
            while(true)
            {
                try
                {
                    m_action++;
                    switch (m_action % 4)
                    {
                        case 0:
                            await AppendOrder();
                            break;
                        case 1:
                            StartOrder();
                            break;
                        case 2:
                            await RequestOrder();
                            break;
                        case 3:
                            await RequestInvalidOrder();
                            break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR: Failed to execute action: " + e.Message);
                } 
                finally
                {
                    Thread.Sleep(1000);
                }
            }
        }

        private async Task AppendOrder()
        {
            Console.WriteLine("INFO : Appending new order ...");
            m_beverage++;
            List<string> beverages = ["Unknown mystical beverage!!", "Espresso", "Café Creme", "Choc", "Latte Macchiato"];
            var strBeverage = beverages[m_beverage % 5];
            m_currentId = await m_client.AppendOrder(new Order{ Name = strBeverage });
        }

        private void StartOrder()
        {
            Console.WriteLine("INFO : Starting order " + m_currentId + " ...");
            m_client.StartOrder(m_currentId);
        }

        private async Task RequestOrder()
        {
            Console.WriteLine("INFO : Requesting order " + m_currentId + " ...");
            var order = await m_client.GetOrder(m_currentId);
            Console.WriteLine("INFO : Received order: id=" + order.Id + ", name=" + order.Name + ", state=" + order.State.ToString());
        }

        private async Task RequestInvalidOrder()
        {
            try
            {
                Console.WriteLine("INFO : Requesting invalid order " + -1 + " ...");
                var order = await m_client.GetOrder(-1);
            } catch(OrderNotFoundException ex)
            {
                Console.WriteLine("INFO : Caught " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void OnOrderStateChanged(int a_id, Order.EState a_state)
        {
            Console.WriteLine("INFO : State of order " + a_id + " changed to " + a_state.ToString());
        }

        private int m_currentId = 0;
        private int m_action = -1;
        private int m_beverage = -1;
        private ReceptionClient m_client;
    }
}
