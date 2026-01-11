using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JsonRpc {
    public class ExceptionConverter {
        public void Register<T>(string typeName, Func<T, JsonObject> encoder, Func<JsonObject, T> decoder) where T : Exception {
            using (new WriteContext(m_lock)) {
                m_typeNames.Add(typeof(T).GUID, typeName);
                m_encoders[typeName] = (ex) => { return encoder((T)ex); };
                m_decoders[typeName] = (ex) => { return decoder(ex); };
            }
        }

        public JsonRpcException Encode(Exception ex) {
            using (new ReadContext(m_lock)) {
                if (ex is TargetInvocationException && ex.InnerException != null)
                    ex = ex.InnerException;

                if (ex is JsonRpcException)
                    return (JsonRpcException)ex;

                if (!m_typeNames.ContainsKey(ex.GetType().GUID))
                    return new JsonRpcException(JsonRpcException.ErrorCode.exception_encoding_failed, nameof(ex) + " is not registered");

                var name = m_typeNames[ex.GetType().GUID];
                var msg = name + ": " + ex.Message;

                try {
                    JsonNode data = new JsonObject { { "name", name }, { "json", m_encoders[name].Invoke(ex) } };
                    return new JsonRpcException(JsonRpcException.ErrorCode.encoded_exception, msg, data);
                } catch (Exception) {
                    return new JsonRpcException(JsonRpcException.ErrorCode.exception_encoding_failed, msg);
                }
            }
        }

        public Exception Decode(JsonRpcException ex) {
            using (new ReadContext(m_lock)) {
                if (ex.Code != JsonRpcException.ErrorCode.encoded_exception)
                    return ex;

                try {
                    var obj = ex.Data.AsObject();
                    var name = obj["name"].GetValue<string>();
                    return m_decoders[name].Invoke(obj["json"].Deserialize<JsonObject>());
                } catch (Exception) {
                    return new JsonRpcException(JsonRpcException.ErrorCode.exception_decoding_failed, ex.Message);
                }
            }
        }

        ReaderWriterLock m_lock = new();
        private Dictionary<Guid, string> m_typeNames = [];
        private Dictionary<string, Func<Exception, JsonObject>> m_encoders = [];
        private Dictionary<string, Func<JsonObject, Exception>> m_decoders = [];
    }
}
