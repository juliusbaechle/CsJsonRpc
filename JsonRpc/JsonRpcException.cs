using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace JsonRpc
{
    [Serializable]
    public class JsonRpcException : Exception
    {
        public JsonRpcException()
        { }

        public JsonRpcException(ErrorCode a_code, string a_msg, JsonElement? a_data = null)
            : base(a_msg)
        { }

        public JsonRpcException(string message, Exception innerException)
            : base(message, innerException)
        { }

        public ErrorCode Code { get; set; }
        public new JsonElement? Data { get; set; } = null;
        public new string Message { get; set; } = "";

        public override string ToString()
        {
            string str = Code.ToString() + ": " + Message;
            if (Data != null)
                str += ", data: " + Data.ToString();
            return str;
        }

        public enum ErrorCode
        {
            parse_error,
            invalid_request,
            method_not_found,
            invalid_params,
            internal_error,
            connection_error,
            subscription_not_found,
            nothing_to_unsubscribe,
            subscription_not_registered,
            encoded_exception,
            exception_encoding_failed,
            exception_decoding_failed,
            sync_call_timeout,
            invalid_result
        }
    }
}
