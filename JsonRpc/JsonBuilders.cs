using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JsonRpc {
    public class JsonBuilders {
        public static JsonNode Request(JsonNode? a_id, string a_methodName, JsonNode? a_params) {
            return new JsonObject { { "jsonrpc", "2.0" }, { "method", a_methodName }, { "params", a_params }, { "id", a_id } };
        }

        public static JsonNode Request(JsonNode? a_id, string a_methodName) {
            return new JsonObject { { "jsonrpc", "2.0" }, { "method", a_methodName }, { "id", a_id } };
        }

        public static JsonNode Notify(string a_methodName, JsonNode? a_params) {
            return new JsonObject { { "jsonrpc", "2.0" }, { "method", a_methodName }, { "params", a_params } };
        }

        public static JsonNode Notify(string a_methodName) {
            return new JsonObject { { "jsonrpc", "2.0" }, { "method", a_methodName } };
        }

        public static JsonNode Response(JsonNode? a_id, JsonNode a_result) {
            return new JsonObject { { "jsonrpc", "2.0" }, { "id", a_id }, { "result", a_result } };
        }

        public static JsonNode Response(JsonNode? a_id, JsonRpcException a_exception) {
            return new JsonObject { { "jsonrpc", "2.0" }, { "error", a_exception }, { "id", a_id } };
        }
    }
}
