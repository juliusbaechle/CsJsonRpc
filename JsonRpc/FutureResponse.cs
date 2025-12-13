using System.Text.Json.Nodes;

namespace JsonRpc
{
    public class FutureResponse
    {
        public bool IsCompleted { get { return m_completed; } }
        public bool IsCompletedSuccessfully { get { return m_succeeded; } }
        public JsonObject Result { get { return m_result; } set
            {
                Interlocked.Exchange(ref m_result, value);
                m_completed = true;
                m_succeeded = true;
                Completed?.Invoke();
            }
        }
        public Exception Exception { get { return m_exception; } set
            {
                Interlocked.Exchange(ref m_exception, value);
                m_completed = true;
                m_succeeded = false;
                Completed?.Invoke();
            } 
        }
        public JsonObject? Id { get; set; }

        public event Action? Completed;

        volatile bool m_completed = false;
        volatile bool m_succeeded = false;
        private JsonObject m_result = new();
        private Exception m_exception = new();
    }
}
