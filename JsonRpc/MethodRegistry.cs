using System.Text.Json;
using System.Text.Json.Nodes;

namespace JsonRpc
{
    public class MethodRegistry
    {
        public void Add(string a_methodName, Delegate a_delegate, List<string>? a_mapping = null)
        {
            m_lock.AcquireWriterLock(0);
            if (m_methods.ContainsKey(a_methodName))
                throw new JsonRpcException(JsonRpcException.ErrorCode.internal_error, "'" + a_methodName + "' is already registered");

            m_methods[a_methodName] = a_delegate;
            if (a_mapping != null)
                m_paramMappings[a_methodName] = a_mapping;
            m_lock.ReleaseWriterLock();
        }

        public bool Contains(string a_methodName)
        {
            m_lock.AcquireReaderLock(0);
            var contained = m_methods.ContainsKey(a_methodName);
            m_lock.ReleaseReaderLock();
            return contained;
        }

        public void Remove(string a_methodName)
        {
            m_lock.AcquireWriterLock(0);
            if (!m_methods.ContainsKey(a_methodName))
                throw new JsonRpcException(JsonRpcException.ErrorCode.internal_error, "'" + a_methodName + "' is not registered");

            m_methods.Remove(a_methodName);
            if (m_paramMappings.ContainsKey(a_methodName))
                m_paramMappings.Remove(a_methodName);
            m_lock.ReleaseWriterLock();
        }

        public JsonNode? Process(string a_methodName, JsonNode? a_params)
        {
            m_lock.AcquireReaderLock(0);
            Delegate? method = null;
            if (!m_methods.TryGetValue(a_methodName, out method))
                throw new JsonRpcException(JsonRpcException.ErrorCode.method_not_found, a_methodName);
            m_lock.ReleaseReaderLock();

            var json_params = NormalizeParameters(a_methodName, a_params);
            var parameters = ParamConverter.Convert(json_params, method.Method.GetParameters());
            var result = method.DynamicInvoke(parameters);
            return JsonSerializer.SerializeToNode(result);
        }

        private JsonArray? NormalizeParameters(string a_methodName, JsonNode? a_params)
        {
            if (a_params == null || a_params.GetValueKind() == JsonValueKind.Null)
                return [];

            if (a_params.GetValueKind() == JsonValueKind.Array)
                return a_params.Deserialize<JsonArray>();

            if (a_params.GetValueKind() == JsonValueKind.Object)
                return NormalizeObjectParameters(a_methodName, a_params);

            throw new JsonRpcException(JsonRpcException.ErrorCode.invalid_request, "params field must be an array, object or null");
        }

        private JsonArray NormalizeObjectParameters(string a_methodName, JsonNode a_params)
        {
            m_lock.AcquireReaderLock(0);
            var paramMappings = m_paramMappings;
            m_lock.ReleaseReaderLock();

            var obj = a_params.Deserialize<JsonObject>();
            if (!paramMappings.ContainsKey(a_methodName))
                throw new JsonRpcException(JsonRpcException.ErrorCode.invalid_params, "procedure doesn't support named parameter");

            JsonArray parameters = [];
            foreach (var parameter in paramMappings[a_methodName])
            {
                if (!obj.ContainsKey(parameter))
                    throw new JsonRpcException(JsonRpcException.ErrorCode.invalid_params, "missing named parameter \"" + parameter + "\"");
                var param = obj[parameter];
                parameters.Add(param?.DeepClone());
            }
            return parameters;
        }

        private ReaderWriterLock m_lock = new();
        private Dictionary<string, Delegate> m_methods = [];
        private Dictionary<string, List<string>> m_paramMappings = [];
    }
}
