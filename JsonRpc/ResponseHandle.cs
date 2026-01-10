using System.Text.Json;
using System.Text.Json.Nodes;

namespace JsonRpc
{
    public interface IResponseHandle
    {
        public void SetResult(JsonNode a_result);

        public void SetException(Exception a_ex);

        public void SetCanceled();
    }

    public class ResponseHandle : IResponseHandle
    {
        public ResponseHandle()
        {
            m_source = new TaskCompletionSource();
        }

        public void SetResult(JsonNode a_result)
        {
            if (a_result != null)
                Console.WriteLine("INFO: Discarded result \"" + a_result + "\"");
            m_source.SetResult();
        }

        public void SetException(Exception a_ex)
        {
            m_source.SetException(a_ex);
        }

        public void SetCanceled()
        {
            m_source.SetCanceled();
        }

        public Task Task { get { return m_source.Task; } }

        TaskCompletionSource m_source;
    }

    public class ResponseHandle<T> : IResponseHandle
    {
        public ResponseHandle()
        {
            m_source = new TaskCompletionSource<T>();
        }

        public void SetResult(JsonNode a_result)
        {
            m_source.SetResult(a_result.Deserialize<T>());
        }

        public void SetException(Exception a_ex)
        {
            m_source.SetException(a_ex);
        }

        public void SetCanceled()
        {
            m_source.SetCanceled();
        }

        public Task<T> Task { get { return m_source.Task; } }

        TaskCompletionSource<T> m_source;
    }
}
