using System.Text.Json.Nodes;

namespace JsonRpc
{
    public class ExceptionConverter
    {
        public void RegisterException<T>(string typeName, Func<Exception, JsonNode> encoder, Func<JsonNode, Exception> decoder)
        {
            m_lock.AcquireWriterLock(0);
            m_typeNames.Add(typeof(T).GUID, typeName);
            m_encoders[typeName] = encoder;
            m_decoders[typeName] = decoder;
            m_lock.ReleaseWriterLock();
        }

        public JsonRpcException Encode(Exception ex)
        {
            m_lock.AcquireReaderLock(0);
            var typeNames = m_typeNames;
            var encoders = m_encoders;
            m_lock.ReleaseReaderLock();

            if (ex is JsonRpcException)
                return (JsonRpcException) ex;

            if (!typeNames.ContainsKey(ex.GetType().GUID))
                return new JsonRpcException(JsonRpcException.ErrorCode.exception_encoding_failed, nameof(ex) + " is unregistered");

            var name = typeNames[ex.GetType().GUID];
            var msg = name + ": " + ex.Message;
            
            try
            {
                JsonNode data = new JsonObject { { "name", name }, { "json", encoders[name].Invoke(ex) } };
                return new JsonRpcException(JsonRpcException.ErrorCode.encoded_exception, msg, data);
            } catch (Exception)
            {
                return new JsonRpcException(JsonRpcException.ErrorCode.exception_encoding_failed, msg);
            }
        }

        public Exception Decode(JsonRpcException ex)
        {
            m_lock.AcquireReaderLock(0);
            var decoders = m_decoders;
            m_lock.ReleaseReaderLock();

            if (ex.Code != JsonRpcException.ErrorCode.encoded_exception)
                return ex;

            try
            {
                var obj = ex.Data.AsObject();
                var name = obj["name"].GetValue<string>();
                return decoders[name].Invoke(obj["json"]);
            } catch (Exception)
            {
                return new JsonRpcException(JsonRpcException.ErrorCode.exception_decoding_failed, ex.Message);
            }
        }

        ReaderWriterLock m_lock = new();
        private Dictionary<Guid, string> m_typeNames = [];
        private Dictionary<string, Func<Exception, JsonNode>> m_encoders = [];
        private Dictionary<string, Func<JsonNode, Exception>> m_decoders = [];
    }
}
