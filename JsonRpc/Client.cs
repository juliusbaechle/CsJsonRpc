using System.Reflection.Metadata;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JsonRpc {
    public class Client : IDisposable {
        public Client(IActiveSocket a_socket, ExceptionConverter a_exceptionConverter) {
            m_socket = a_socket;
            m_exceptionConverter = a_exceptionConverter;
            m_socket.ReceivedMsg += HandleResponse;
        }

        public Task ConnectAsync() {
            return m_socket.ConnectAsync();
        }

        public void Dispose() {
            m_socket.ReceivedMsg -= HandleResponse;
            lock (m_mutex) {
                foreach (var r in m_backlog.Values)
                    r.SetCanceled();
                m_backlog.Clear();
            }
        }

        public void Notify(string a_method) {
            var request = JsonSerializer.Serialize(JsonBuilders.Notify(a_method));
            m_socket.Send(request);
        }

        public void Notify(string a_method, JsonNode a_params) {
            var request = JsonSerializer.Serialize(JsonBuilders.Notify(a_method, a_params));
            m_socket.Send(request);
        }

        public Task Request(string a_method, JsonNode? a_params = null) {
            int id = m_nextId++;
            var handle = new ResponseHandle();
            AppendHandle(id, handle);
            var request = JsonSerializer.Serialize(JsonBuilders.Request(id, a_method, a_params));
            m_socket.Send(request);
            return handle.Task;
        }

        public Task<T> Request<T>(string a_method, JsonNode? a_params = null) {
            int id = m_nextId++;
            var handle = new ResponseHandle<T>();
            AppendHandle(id, handle);
            var request = JsonSerializer.Serialize(JsonBuilders.Request(id, a_method, a_params));
            m_socket.Send(request);
            return handle.Task;
        }

        private void AppendHandle(int a_id, IResponseHandle a_requestHandle) {
            lock (m_mutex) {
                m_backlog[a_id] = a_requestHandle;
            }
        }

        private void HandleResponse(string a_response) {
            JsonObject? response = null;
            try {
                var json = JsonDocument.Parse(a_response);
                response = json.Deserialize<JsonObject>();
                if (response == null) {
                    Logging.LogInfo("Response was null");
                    return;
                }
            } catch (JsonException ex) {
                Logging.LogError("Invalid server response: " + ex.Message);
                return;
            }

            var handle = TakeHandle(response["id"].GetValue<int>());
            if (handle == null) {
                Logging.LogError("Invalid server response: backlog doesn't contain handle with corresponding id");
                return;
            }

            try {
                if (response.ContainsKey("result") == response.ContainsKey("error")) {
                    handle.SetException(new JsonRpcException(JsonRpcException.ErrorCode.invalid_result, """either "result" or "error" field must be contained"""));
                } else if (!response.ContainsKey("jsonrpc") || response["jsonrpc"].GetValue<string>() != "2.0") {
                    handle.SetException(new JsonRpcException(JsonRpcException.ErrorCode.invalid_result, """ "jsonrpc" field must be "2.0" """));
                } else if (response.ContainsKey("error") && response["error"].GetValueKind() == JsonValueKind.Object) {
                    handle.SetException(m_exceptionConverter.Decode(response["error"]));
                } else {
                    handle.SetResult(response["result"]);
                }
            } catch (Exception ex) {
                handle.SetException(ex);
            }
        }

        private IResponseHandle? TakeHandle(int a_id) {
            lock (m_mutex) {
                if (m_backlog.ContainsKey(a_id)) {
                    var handle = m_backlog[a_id];
                    m_backlog.Remove(a_id);
                    return handle;
                } else {
                    return null;
                }
            }
        }

        private Mutex m_mutex = new();
        private IActiveSocket m_socket;
        private ExceptionConverter m_exceptionConverter;
        private Dictionary<int, IResponseHandle> m_backlog = [];
        private volatile int m_nextId = 0;
    }
}
