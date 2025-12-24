using System.Text.Json;
using System.Text.Json.Nodes;

namespace JsonRpc
{
    public class MethodRegistry
    {
        public void Add(string a_methodName, Delegate a_delegate, List<string>? a_mapping = null)
        {
            if (m_methods.ContainsKey(a_methodName))
                throw new JsonRpcException(JsonRpcException.ErrorCode.internal_error, "'" + a_methodName + "' is already registered");

            m_methods[a_methodName] = a_delegate;
            if (a_mapping != null)
                m_paramMappings[a_methodName] = a_mapping;
        }

        public bool Contains(string a_methodName)
        {
            return m_methods.ContainsKey(a_methodName);
        }

        public void Remove(string a_methodName)
        {
            if (!m_methods.ContainsKey(a_methodName))
                throw new JsonRpcException(JsonRpcException.ErrorCode.internal_error, "'" + a_methodName + "' is not registered");

            m_methods.Remove(a_methodName);
            if (m_paramMappings.ContainsKey(a_methodName))
                m_paramMappings.Remove(a_methodName);
        }

        public JsonNode? Process(string a_methodName, JsonNode? a_params)
        {
            if (!m_methods.ContainsKey(a_methodName))
                throw new JsonRpcException(JsonRpcException.ErrorCode.method_not_found, a_methodName);

            var json_params = NormalizeParameters(a_methodName, a_params);
            var parameters = ParamConverter.Convert(json_params, m_methods[a_methodName].Method.GetParameters());
            var result = m_methods[a_methodName].DynamicInvoke(parameters);
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
            var obj = a_params.Deserialize<JsonObject>();
            if (!m_paramMappings.ContainsKey(a_methodName))
                throw new JsonRpcException(JsonRpcException.ErrorCode.invalid_params, "procedure doesn't support named parameter");

            JsonArray parameters = [];
            foreach (var parameter in m_paramMappings[a_methodName])
            {
                if (!obj.ContainsKey(parameter))
                    throw new JsonRpcException(JsonRpcException.ErrorCode.invalid_params, "missing named parameter \"" + parameter + "\"");
                var param = obj[parameter];
                parameters.Add(param?.DeepClone());
            }
            return parameters;
        }

        private Dictionary<string, Delegate> m_methods = [];
        private Dictionary<string, List<string>> m_paramMappings = [];
    }
}
