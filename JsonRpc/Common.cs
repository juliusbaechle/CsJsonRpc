using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JsonRpc
{
    internal class Common
    {
        public static T GetValue<T>(JsonObject a_json, string a_key) {
            if (!a_json.ContainsKey(a_key))
                throw new JsonRpcException(JsonRpcException.ErrorCode.internal_error, a_json + " does not contain key " + a_key);
            
            var obj = a_json[a_key];
            if (obj == null)
                throw new JsonRpcException(JsonRpcException.ErrorCode.internal_error, a_key + " is null in " + a_json);
            
            try
            {
                return obj.GetValue<T>();
            } catch
            {
                throw new JsonRpcException(JsonRpcException.ErrorCode.internal_error, "could not deserialize " + obj + " as " + typeof(T));
            }
        }
    }
}
