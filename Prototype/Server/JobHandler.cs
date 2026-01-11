using System.Timers;
using Reception_Common;

namespace Server {
    internal class JobHandler : IDisposable {
        public JobHandler(Reception a_reception) {
            m_reception = a_reception;
            m_reception.OrderStateChanged += OnOrderState;
            m_timer.Elapsed += OnTimeout;
        }

        public void Dispose() {
            m_timer.Stop();
            m_timer.Dispose();
        }

        private void OnOrderState(int a_id, Order.EState a_state) {
            if (a_state == Order.EState.InProgress)
                StartTimer();
        }

        private void OnTimeout(object? sender, ElapsedEventArgs e) {
            var allOrders = m_reception.GetAllOrders();
            List<Order> ordersInProgress = [];
            foreach (Order o in allOrders)
                if (o.State == Order.EState.InProgress)
                    ordersInProgress.Add(o);

            if (ordersInProgress.Count >= 1)
                m_reception.SetOrderState(ordersInProgress[0].Id, Order.EState.Finished);

            if (ordersInProgress.Count >= 2)
                StartTimer();
        }

        private void StartTimer() {
            if (!m_timer.Enabled) {
                m_timer.Interval = 5000 + new Random().Next(0, 15000);
                m_timer.Start();
            }
        }

        Reception m_reception;
        System.Timers.Timer m_timer = new System.Timers.Timer(10000);
    }
}
