using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JsonRpc
{
    public class RequestProcessor
    {
        public RequestProcessor(MethodRegistry a_registry, ExceptionConverter a_exceptionConverter)
        {
            m_registry = a_registry;
            m_exceptionConverter = a_exceptionConverter;
        }

        public string HandleRequest(string a_request) {
            try
            {
                var request = JsonDocument.Parse(a_request);
                if (request.RootElement.ValueKind == JsonValueKind.Array)
                {
                    var arr_request = request.RootElement.Deserialize<JsonArray>();
                    var result = new JsonArray();
                    foreach (var r in arr_request) {
                        var res = HandleSingleRequest(r.Deserialize<JsonObject>());
                        if (res != null)
                            result.Add(res);
                    }
                    return JsonSerializer.Serialize(result);
                }
                else if (request.RootElement.ValueKind == JsonValueKind.Object)
                {
                    var res = HandleSingleRequest(request.RootElement.Deserialize<JsonObject>());
                    return JsonSerializer.Serialize(res);
                }
                else
                {
                    return """{"id":null, "error":{"code":-32600, "message": "invalid request: expected array or object"}, "jsonrpc":"2.0"}""";
                }
            } catch (JsonException)
            {
                return """{"id":null, "error":{"code":-32700, "message": "parse error"}, "jsonrpc":"2.0"}""";
            }
        }

        private JsonNode? HandleSingleRequest(JsonObject a_request)
        {
            var id = (JsonNode?) null;
            if (HasValidId(a_request))
            {
                id = a_request["id"];
                if (id != null)
                    id = id.DeepClone();
            }
            
            try
            {
                return ProcessSingleRequest(a_request);
            } catch (Exception ex)
            {
                return JsonBuilders.Response(id, m_exceptionConverter.Encode(ex));
            }
        }

        static bool HasKeyType(JsonObject obj, string key, JsonValueKind kind)
        {
            if (!obj.ContainsKey(key))
                return false;
            var node = obj[key];
            if (node == null)
                return false;
            if (node.GetValueKind() != kind)
                return false;
            return true;
        }

        private JsonNode? ProcessSingleRequest(JsonObject a_request)
        {
            if (!HasKeyType(a_request, "jsonrpc", JsonValueKind.String) || a_request["jsonrpc"].Deserialize<string>() != "2.0")
                throw new JsonRpcException(JsonRpcException.ErrorCode.invalid_request, """invalid request: missing jsonrpc field set to "2.0" """);
            if (!HasKeyType(a_request, "method", JsonValueKind.String))
                throw new JsonRpcException(JsonRpcException.ErrorCode.invalid_request, """invalid request: method field must be a string""");
            if (a_request.ContainsKey("id") && !HasValidId(a_request))
                throw new JsonRpcException(JsonRpcException.ErrorCode.invalid_request, """invalid request: id field must be a number, string or null""");
            if (a_request.ContainsKey("params") && !(a_request["params"] == null || a_request["params"].GetValueKind() == JsonValueKind.Array || a_request["params"].GetValueKind() == JsonValueKind.Object))
                throw new JsonRpcException(JsonRpcException.ErrorCode.invalid_request, """invalid request: params field must be an array, object or null""");
            if (!a_request.ContainsKey("params") || HasKeyType(a_request, "params", JsonValueKind.Null))
                a_request["params"] = new JsonArray();
            if (!a_request.ContainsKey("id"))
            {
                m_registry.Process(a_request["method"].Deserialize<string>(), a_request["params"]);
                return null;
            } else
            {
                var response = new JsonObject();
                response["jsonrpc"] = "2.0";
                response["id"] = a_request["id"]?.DeepClone();
                response["result"] = m_registry.Process(a_request["method"].Deserialize<string>(), a_request["params"]);
                return response;
            }
        }

        private bool HasValidId(JsonObject a_request)
        {
            if (!a_request.ContainsKey("id"))
                return false;
            
            var id = a_request["id"];
            if (id == null)
                return true;

            var type = id.GetValueKind();
            return type == JsonValueKind.String || type == JsonValueKind.Number;
        }

        private MethodRegistry m_registry;
        private ExceptionConverter m_exceptionConverter;
    }
}
