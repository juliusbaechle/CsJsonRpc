using Reception_Common;

namespace Client
{
    public interface IReceptionClient : IDisposable
    {
        public Task<int> AppendOrder(Order a_order);

        /// May throw OrderNotFoundException
        public void StartOrder(int a_id);

        /// May throw OrderNotFoundException
        public Task<Order> GetOrder(int a_id);

        public event Action<int, Order.EState> OrderStateChanged;
    }
}