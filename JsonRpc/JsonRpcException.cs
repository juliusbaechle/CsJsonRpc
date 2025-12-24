using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JsonRpc
{
    [Serializable]
    public class JsonRpcException : Exception
    {
        public JsonRpcException(ErrorCode a_code, string a_msg)
            : base(a_msg)
        {
            Code = a_code;
            Message = a_msg;
        }

        public JsonRpcException(ErrorCode a_code, string? a_msg, JsonNode? a_data)
            : base(a_msg)
        {
            Code = a_code;
            Message = a_msg;
            Data = a_data;
        }

        public ErrorCode Code { get; set; }
        public new JsonNode? Data { get; set; } = null;
        public new string Message { get; set; } = "";

        static public implicit operator JsonRpcException(JsonNode a_json)
        {
            try
            {
                var obj = a_json.Deserialize<JsonObject>();
                if (obj == null)
                    return new JsonRpcException(ErrorCode.internal_error, a_json + " is null");

                var code = obj["code"];
                var msg = obj["message"];
                var data = obj["data"];

                if (code == null || msg == null)
                    return new JsonRpcException(ErrorCode.internal_error, "failed to deserialize " + a_json + " into JsonRpcException");

                return new JsonRpcException(code.Deserialize<ErrorCode>(), msg.Deserialize<string>(), data.Deserialize<JsonNode>());
            } catch
            {
                return new JsonRpcException(ErrorCode.internal_error, "failed to deserialize " + a_json + " into JsonRpcException");
            }
        }

        static public implicit operator JsonNode(JsonRpcException ex)
        {
            JsonObject obj = new();
            obj["code"] = (int) ex.Code;
            obj["message"] = ex.Message;
            if (ex.Data != null)
                obj["data"] = ex.Data;
            return obj;
        }

        public enum ErrorCode
        {
            parse_error = -32700,
            invalid_request = -32600,
            method_not_found = -32601,
            invalid_params = -32602,
            internal_error = -32603,
            server_error = -32000,
            invalid
        }
    }
}
