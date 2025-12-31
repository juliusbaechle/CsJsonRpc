using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JsonRpc
{
    public class Client : IDisposable
    {
        public Client(IActiveSocket a_socket) {
            m_socket = a_socket;
            m_exceptionConverter = new();
            m_socket.ReceivedMsg += HandleResponse;
        }

        public Client(IActiveSocket a_socket, ExceptionConverter a_exceptionConverter)
        {
            m_socket = a_socket;
            m_exceptionConverter = a_exceptionConverter;
        }

        public void Dispose()
        {
            m_socket.ReceivedMsg -= HandleResponse;
            m_mutex.WaitOne();
            m_backlog.Clear();
            m_mutex.ReleaseMutex();
        }

        public void Notify(string a_method)
        {
            var request = JsonSerializer.Serialize(JsonBuilders.Notify(a_method));
            m_socket.Send(request);
        }

        public void Notify(string a_method, JsonNode a_params)
        {
            var request = JsonSerializer.Serialize(JsonBuilders.Notify(a_method, a_params));
            m_socket.Send(request);
        }

        public int Request<T>(string a_method, Action<T> a_resultCallback, Action<Exception>? a_errorCallback = null)
        {
            int id = m_nextId++;
            AppendHandle(id, new RequestHandle<T>(a_resultCallback, a_errorCallback));
            var request = JsonSerializer.Serialize(JsonBuilders.Request(id, a_method));
            m_socket.Send(request);
            return id;
        }

        public int Request(string a_method, Action a_resultCallback, Action<Exception>? a_errorCallback = null)
        {
            int id = m_nextId++;
            AppendHandle(id, new RequestHandle(a_resultCallback, a_errorCallback));
            var request = JsonSerializer.Serialize(JsonBuilders.Request(id, a_method));
            m_socket.Send(request);
            return id;
        }

        public int Request<T>(string a_method, JsonNode a_params, Action<T> a_resultCallback, Action<Exception>? a_errorCallback = null)
        {
            int id = m_nextId++;
            AppendHandle(id, new RequestHandle<T>(a_resultCallback, a_errorCallback));
            var request = JsonSerializer.Serialize(JsonBuilders.Request(id, a_method, a_params));
            m_socket.Send(request);
            return id;
        }

        public int Request(string a_method, JsonNode a_params, Action a_resultCallback, Action<Exception>? a_errorCallback = null)
        {
            int id = m_nextId++;
            AppendHandle(id, new RequestHandle(a_resultCallback, a_errorCallback));
            var request = JsonSerializer.Serialize(JsonBuilders.Request(id, a_method, a_params));
            m_socket.Send(request);
            return id;
        }

        private void AppendHandle(int a_id, RequestHandleBase a_requestHandle)
        {
            m_mutex.WaitOne();
            m_backlog[a_id] = a_requestHandle;
            m_mutex.ReleaseMutex();
        }

        private void HandleResponse(string a_response)
        {
            JsonObject? response  = null;
            try
            {
                var json = JsonDocument.Parse(a_response);
                response = json.Deserialize<JsonObject>();
                if (response == null)
                    throw new JsonRpcException(JsonRpcException.ErrorCode.parse_error, "response was null");
            } catch (JsonException ex) 
            {
                Console.WriteLine("ERROR: invalid server response: " + ex.Message);
                return;
            }

            var handle = TakeHandle(response["id"].GetValue<int>());
            if (handle == null)
            {
                Console.WriteLine("ERROR: invalid server response: backlog doesn't contain handle with corresponding id");
                return;
            }

            try
            {
                if (response.ContainsKey("result") == response.ContainsKey("error"))
                {
                    handle.HandleException(new JsonRpcException(JsonRpcException.ErrorCode.invalid_result, """either "result" or "error" field must be contained"""));
                }
                else if (!response.ContainsKey("jsonrpc") || response["jsonrpc"].GetValue<string>() != "2.0")
                {
                    handle.HandleException(new JsonRpcException(JsonRpcException.ErrorCode.invalid_result, """ "jsonrpc" field must be "2.0" """));
                }
                else if (response.ContainsKey("error") && response["error"].GetValueKind() == JsonValueKind.Object)
                {
                    handle.HandleException(m_exceptionConverter.Decode(response["error"]));
                } else
                {
                    handle.HandleResult(response["result"]);
                }
            } catch(Exception ex)
            {
                handle.HandleException(ex);
            }
        }

        private RequestHandleBase? TakeHandle(int a_id)
        {
            RequestHandleBase? handle = null;
            m_mutex.WaitOne();
            if (m_backlog.ContainsKey(a_id))
            {
                handle = m_backlog[a_id];
                m_backlog.Remove(a_id);
            }
            m_mutex.ReleaseMutex();
            return handle;
        }

        private Mutex m_mutex = new();
        private IActiveSocket m_socket;
        private ExceptionConverter m_exceptionConverter;
        private Dictionary<int, RequestHandleBase> m_backlog = [];
        private volatile int m_nextId = 0;
    }
}
