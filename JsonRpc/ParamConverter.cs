using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace JsonRpc
{
    internal class ParamConverter
    {
        public static object?[]? Convert(JsonArray a_params, ParameterInfo[] a_infos)
        {
            if (a_infos.Length != a_params.Count)
                throw new JsonRpcException(JsonRpcException.ErrorCode.invalid_params, "wrong parameter count");

            List<object?> converted_params = [];
            for (int i = 0; i < a_params.Count; i++)
            {
                var type = a_infos[i].ParameterType;
                try
                {
                    var converted_param = JsonSerializer.Deserialize(a_params[i], type);
                    converted_params.Add(converted_param);
                } catch (JsonException)
                {
                    throw new JsonRpcException(JsonRpcException.ErrorCode.invalid_params, "failed to convert " + a_params[i] + " to " + type);
                }
            }
            return converted_params.ToArray();
        }
    }
}
