using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JsonRpc
{
    public class JsonBuilders
    {
        public static string Request(JsonNode a_id, string a_methodName, JsonNode a_params)
        {
            var json = new JsonObject { { "jsonrpc", "2.0" }, { "method", a_methodName }, { "params", a_params }, { "id", a_id } };
            return JsonSerializer.Serialize(json);
        }

        public static string Request(JsonNode a_id, string a_methodName)
        {
            var json = new JsonObject { { "jsonrpc", "2.0" }, { "method", a_methodName }, { "id", a_id } };
            return JsonSerializer.Serialize(json);
        }

        public static string Notify(string a_methodName, JsonNode a_params)
        {
            var json = new JsonObject { { "jsonrpc", "2.0" }, { "method", a_methodName }, { "params", a_params } };
            return JsonSerializer.Serialize(json);
        }

        public static string Notify(string a_methodName)
        {
            var json = new JsonObject { { "jsonrpc", "2.0" }, { "method", a_methodName } };
            return JsonSerializer.Serialize(json);
        }

        public static string Response(JsonNode a_id, JsonNode a_result)
        {
            var json = new JsonObject { { "jsonrpc", "2.0" }, { "result", a_result }, { "id", a_id } };
            return JsonSerializer.Serialize(json);
        }

        public static string Response(JsonNode a_id, JsonRpcException a_exception)
        {
            var json = new JsonObject { { "jsonrpc", "2.0" }, { "error", a_exception }, { "id", a_id } };
            return JsonSerializer.Serialize(json);
        }
    }
}
