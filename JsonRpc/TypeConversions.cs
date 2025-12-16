using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JsonRpc
{
    internal class TypeConversions
    {
        public static object ConvertTo(JsonNode? node, Type type)
        {
            if (node == null)
                throw new ArgumentNullException();
            var methodInfo = typeof(JsonNode).GetMethod("GetValue");
            var genericMethodInfo = methodInfo?.MakeGenericMethod(new[] { type });
            try
            {
                var result = genericMethodInfo?.Invoke(node, []);
                if (result == null)
                    throw new JsonRpcException(JsonRpcException.ErrorCode.invalid_params, "couldn't convert to " + type);
                return result;
            } catch (Exception ex)
            {
                throw new JsonRpcException("failed to convert " + node + " to " + type, ex);
            }
        }

        public static T CastTo<T>(object? o) {
            if (o == null)
                throw new ArgumentNullException();
            var result = (T)o;
            if (result == null)
                throw new JsonRpcException(JsonRpcException.ErrorCode.internal_error, "failed to cast " + o + " to " + typeof(T));
            return result;
        }
        public static dynamic CastToReflected(object? o, Type type)
        {
            var methodInfo = typeof(TypeConversions).GetMethod("CastTo");
            var genericMethodInfo = methodInfo?.MakeGenericMethod(new[] { type });
            var result = genericMethodInfo?.Invoke(null, new[] { o });
            if (result == null)
                throw new JsonRpcException(JsonRpcException.ErrorCode.invalid_params, "couldn't cast to " + type);
            return result;
        }
    }
}
