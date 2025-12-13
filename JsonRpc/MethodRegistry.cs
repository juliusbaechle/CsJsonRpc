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
        public static object ConvertTo(JsonNode node, Type type)
        {
            var methodInfo = typeof(JsonNode).GetMethod("GetValue");
            var genericArguments = new[] { type };
            var genericMethodInfo = methodInfo?.MakeGenericMethod(genericArguments);
            return genericMethodInfo?.Invoke(node, []);
        }

        public static T CastTo<T>(object o) { return (T)o; }
        public static dynamic CastToReflected(object o, Type type) {
            var methodInfo = typeof(MethodRegistry).GetMethod("CastTo");
            var genericArguments = new[] { type };
            var genericMethodInfo = methodInfo?.MakeGenericMethod(genericArguments);
            return genericMethodInfo?.Invoke(null, new[] { o });
        }



        public void Add(Delegate func)
        {
            m_methods.Add(func.Method.Name, func);
        }

        public bool Contains(String a_methodName)
        {
            bool result = m_methods.ContainsKey(a_methodName);
            return result;
        }

        public void Remove(Delegate func)
        {
            m_methods.Remove(func.Method.Name);
        }

        public async Task<string> Process(string a_methodName, JsonDocument a_params)
        {
            if (!m_methods.ContainsKey(a_methodName))
                throw new JsonRpcException(JsonRpcException.ErrorCode.method_not_found, a_methodName);
            var func = m_methods[a_methodName];

            try
            {
                var normalized_params = NormalizeParameters(a_methodName, a_params);
                if (func.Method.ReturnType == typeof(Task))
                {
                    var task = (Task) func.DynamicInvoke(normalized_params.ToArray());
                    await task;
                    return "";
                } else if (func.Method.ReturnType.BaseType == typeof(Task)) {
                    var task = CastToReflected(func.DynamicInvoke(normalized_params.ToArray()), func.Method.ReturnType);
                    var result = await task;
                    return JsonSerializer.Serialize(result);
                } else
                {
                    return await Task.Run(() =>
                    {
                        var result = func.DynamicInvoke(normalized_params.ToArray());
                        return JsonSerializer.Serialize(result);
                    });
                }
            } catch(JsonException e)
            {
                throw new JsonRpcException(JsonRpcException.ErrorCode.invalid_params, e.Message);
            } catch(JsonRpcException e)
            {
                throw ProcessTypeError(a_methodName, e);
            }
        }

        private List<object> NormalizeParameters(String a_methodName, JsonDocument a_params)
        {
            List<object> parameters = [];

            if (a_params.RootElement.ValueKind == JsonValueKind.Null)
                return parameters;

            if (a_params.RootElement.ValueKind == JsonValueKind.Array)
            {
                var func = m_methods[a_methodName];
                var arr = a_params.Deserialize<JsonArray>();
                for (int i = 0; i < arr.Count; i++)
                {
                    var type = func.Method.GetParameters()[i].ParameterType;
                    parameters.Add(ConvertTo(arr[i], type));
                }
                return parameters;
            }

            if (a_params.RootElement.ValueKind == JsonValueKind.Object)
            {
                var obj = a_params.Deserialize<JsonObject>();

                foreach (var p in m_methods[a_methodName].Method.GetParameters())
                {
                    if (!obj.ContainsKey(p.Name))
                        throw new JsonRpcException(JsonRpcException.ErrorCode.invalid_params, "missing named parameter \"" + p.Name + "\"");
                    parameters.Add(ConvertTo(obj[p.Name], p.ParameterType));
                }
                return parameters;
            }

            throw new JsonRpcException(JsonRpcException.ErrorCode.invalid_request, "params field must be an array, object or null");
        }

        private JsonRpcException ProcessTypeError(string a_methodName, JsonRpcException a_ex)
        {
            // TODO
            return a_ex;
        }

        private Dictionary<String, Delegate> m_methods = [];
    }
}
