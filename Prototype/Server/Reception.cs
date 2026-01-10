using System.Text.Json;
using Reception_Common;

namespace Server
{
    public class Reception
    {
        public int AppendOrder(Order a_order)
        {
            Console.WriteLine("INFO : Requested AppendOrder( order: " + JsonSerializer.Serialize(a_order) + " ) Assigned id '" + m_nextId.ToString() + "'");
            a_order.Id = m_nextId;
            m_nextId++;
            m_orders.Add(a_order.Id, a_order);
            return a_order.Id;
        }

        public void StartOrder(int a_id)
        {
            Console.WriteLine("INFO : Requested StartOrder( id: " + a_id + " )");
            CheckContained(a_id);
            m_orders[a_id].State = Order.EState.InProgress;
            OrderStateChanged?.Invoke(a_id, Order.EState.InProgress);
        }

        public Order GetOrder(int a_id)
        {
            Console.WriteLine("INFO : Requested GetOrder( id: " + a_id + " )");
            CheckContained(a_id);
            return m_orders[a_id];
        }

        public void SetOrderState(int a_id, Order.EState a_state)
        {
            Console.WriteLine("INFO : Requested SetOrderState( id: " + a_id + " , state: " + a_state.ToString() + " )");
            CheckContained(a_id);
            m_orders[a_id].State = a_state;
            OrderStateChanged?.Invoke(a_id, a_state);
        }

        public List<Order> GetAllOrders() {
            return m_orders.Values.ToList();
        }

        public event Action<int, Order.EState>? OrderStateChanged;

        private void CheckContained(int a_id)
        {
            if (!m_orders.Keys.Contains(a_id))
                throw new OrderNotFoundException(a_id.ToString());
        }

        private Dictionary<int, Order> m_orders = [];
        private int m_nextId;
    }
}
