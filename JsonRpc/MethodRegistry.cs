using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;

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

        public bool Contains(String a_methodName)
        {
            return m_methods.ContainsKey(a_methodName);
        }

        public void Remove(String a_methodName)
        {
            if (!m_methods.ContainsKey(a_methodName))
                throw new JsonRpcException(JsonRpcException.ErrorCode.internal_error, "'" + a_methodName + "' is not registered");

            m_methods.Remove(a_methodName);
            if (m_paramMappings.ContainsKey(a_methodName))
                m_paramMappings.Remove(a_methodName);
        }

        public async Task<string> Process(string a_methodName, JsonDocument a_params)
        {
            if (!m_methods.ContainsKey(a_methodName))
                throw new JsonRpcException(JsonRpcException.ErrorCode.method_not_found, a_methodName);

            var json_params = NormalizeParameters(a_methodName, a_params.RootElement);
            var parameters = ConvertParameters(a_methodName, json_params);
            return await CallDynamic(a_methodName, parameters);
        }

        private JsonArray NormalizeParameters(String a_methodName, JsonElement a_params)
        {
            if (a_params.ValueKind == JsonValueKind.Null)
                return [];

            if (a_params.ValueKind == JsonValueKind.Array)
            {
                var arr = a_params.Deserialize<JsonArray>();
                if (arr == null)
                    throw new JsonRpcException(JsonRpcException.ErrorCode.internal_error, "failed to parse " + a_params);
                return arr;
            }

            if (a_params.ValueKind == JsonValueKind.Object)
            {
                var obj = a_params.Deserialize<JsonObject>();
                if (obj == null)
                    throw new JsonRpcException(JsonRpcException.ErrorCode.invalid_params, "failed to parse " + a_params);

                if (!m_paramMappings.ContainsKey(a_methodName))
                    throw new JsonRpcException(JsonRpcException.ErrorCode.invalid_params, "procedure doesn't support named parameter");

                JsonArray parameters = [];
                foreach (var parameter in m_paramMappings[a_methodName])
                {
                    if (!obj.ContainsKey(parameter))
                        throw new JsonRpcException(JsonRpcException.ErrorCode.invalid_params, "missing named parameter \"" + parameter + "\"");
                    var param = obj[parameter];
                    if (param == null)
                        throw new JsonRpcException(JsonRpcException.ErrorCode.invalid_params, "parameter is null " + a_params);
                    parameters.Add(param.DeepClone());
                }
                return parameters;
            }

            throw new JsonRpcException(JsonRpcException.ErrorCode.invalid_request, "params field must be an array, object or null");
        }

        object[] ConvertParameters(string a_methodName, JsonArray a_params)
        {
            if (m_methods[a_methodName].Method.GetParameters().Length != a_params.Count)
                throw new JsonRpcException(JsonRpcException.ErrorCode.invalid_params, "wrong parameter count");

            var method = m_methods[a_methodName];
            List<object> converted_params = [];
            for (int i = 0; i < a_params.Count; i++)
            {
                var type = method.Method.GetParameters()[i].ParameterType;
                var converted_param = TypeConversions.ConvertTo(a_params[i], type);
                converted_params.Add(converted_param);
            }
            return converted_params.ToArray();
        }

        private async Task<string> CallDynamic(string a_methodName, object[] a_params)
        {
            var method = m_methods[a_methodName];
            if (method.Method.ReturnType == typeof(Task))
            {
                var obj = method.DynamicInvoke(a_params);
                await TypeConversions.CastTo<Task>(obj);
                return "null";
            }
            else if (method.Method.ReturnType.BaseType == typeof(Task))
            {
                var obj = method.DynamicInvoke(a_params);
                var result = await TypeConversions.CastToReflected(obj, method.Method.ReturnType);
                return JsonSerializer.Serialize(result);
            }
            else
            {
                return await Task.Run(() =>
                {
                    var obj = method.DynamicInvoke(a_params);
                    return JsonSerializer.Serialize(obj);
                });
            }
        }

        private Dictionary<String, Delegate> m_methods = [];
        private Dictionary<String, List<String>> m_paramMappings = [];
    }
}
