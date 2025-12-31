using System.Text.Json;
using System.Text.Json.Nodes;

namespace JsonRpc
{
    public abstract class RequestHandleBase
    {
        public RequestHandleBase(Action<Exception>? a_errorCallback = null)
        {
            if (a_errorCallback == null)
                m_errorCallback = DefaultErrorCallback;
            else
                m_errorCallback = a_errorCallback;
        }

        public void HandleException(Exception ex) { m_errorCallback(ex); }
        public abstract void HandleResult(JsonNode node);

        private void DefaultErrorCallback(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        private Action<Exception> m_errorCallback;
    }

    public class RequestHandle<T> : RequestHandleBase
    {
        public RequestHandle(Action<T> a_resultCallback, Action<Exception>? a_errorCallback = null)
            : base(a_errorCallback)
        {
            m_resultCallback = a_resultCallback;
        }

        public override void HandleResult(JsonNode node)
        {
            try
            {
                T result = node.GetValue<T>();
                m_resultCallback(result);
            }
            catch (JsonException ex)
            {
                HandleException(new JsonRpcException(JsonRpcException.ErrorCode.invalid_result, ex.Message));
            }
        }

        private Action<T> m_resultCallback;
    }

    public class RequestHandle : RequestHandleBase
    {
        public RequestHandle(Action a_resultCallback, Action<Exception>? a_errorCallback = null)
            : base(a_errorCallback)
        {
            m_resultCallback = a_resultCallback;
        }

        public override void HandleResult(JsonNode node)
        {
            if (node != null)
                Console.WriteLine("result " + node + " was discarded");
            m_resultCallback();
        }

        private Action m_resultCallback;
    }
}
