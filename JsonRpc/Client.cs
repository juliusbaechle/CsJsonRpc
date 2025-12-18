using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JsonRpc
{
    public enum Version { v1, v2 };
    
    public struct JsonRpcResponse
    {
        public int Id;
        public JsonNode Result;
    }

    public class Client
    {
        public Client(IClientConnector a_connector, Version a_version = Version.v2) {
            m_connector = a_connector;
            m_version = a_version;
        }

        public T Request<T>(int a_id, string a_method)
        {
            return CallMethod<T>(JsonBuilders.Request(a_id, a_method)).Result.GetValue<T>();
        }

        public T Request<T>(int a_id, string a_method, JsonNode a_params)
        {
            return CallMethod<T>(JsonBuilders.Request(a_id, a_method, a_params)).Result.GetValue<T>();
        }

        public void Notify(string a_method)
        {
            m_connector.Send(JsonBuilders.Notify(a_method));
        }

        public void Notify(string a_method, JsonNode a_params)
        {
            m_connector.Send(JsonBuilders.Notify(a_method, a_params));
        }

        private JsonRpcResponse CallMethod<T>(string a_request)
        {
            try
            {
                var json = JsonDocument.Parse(m_connector.Send(a_request));
                var response = json.Deserialize<JsonObject>();
                if (response == null)
                    throw new JsonRpcException(JsonRpcException.ErrorCode.parse_error, "couldn't parse response");

                var error = response["error"];
                if (error != null && error.GetValueKind() == JsonValueKind.Object)
                {
                    throw JsonRpcException.FromJson(error);
                } else if (error != null && error.GetValueKind() == JsonValueKind.String)
                {
                    throw new JsonRpcException(JsonRpcException.ErrorCode.internal_error, error.GetValue<String>());
                }

                var result = response["result"];
                var id = response["id"];
                if (result != null && id != null)
                {
                    var converted_result = result.GetValue<T>();
                    return new JsonRpcResponse{ Id = id.GetValue<int>(), Result = result };
                }

                throw new JsonRpcException(JsonRpcException.ErrorCode.internal_error, """invalid server response: neither "result" nor "error" fields found""");
            } catch (JsonException ex)
            {
                throw new JsonRpcException(JsonRpcException.ErrorCode.parse_error, """Invalid json response from server: """ + ex.Message);
            } catch (InvalidOperationException ex)
            {
                throw new JsonRpcException(JsonRpcException.ErrorCode.parse_error, """Invalid json response from server: """ + ex.Message);
            }
        }

        private IClientConnector m_connector;
        private Version m_version;
    }
}
