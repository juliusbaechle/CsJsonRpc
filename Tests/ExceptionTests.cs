using JsonRpc;
using System;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using System.Text.Json.Nodes;
using Tests.Mocks;

namespace Tests
{
    [Serializable]
    public class MyException : Exception
    {
        public MyException()
        { }

        public MyException(string message)
            : base(message)
        { }

        public MyException(string message, Exception innerException)
            : base(message, innerException)
        { }

        public static implicit operator JsonNode(MyException e)
        {
            var n = new JsonObject();
            n["message"] = e.Message;
            return n;
        }

        public static implicit operator MyException(JsonNode n)
        {
            var o = n.AsObject();
            var msg = o["message"].GetValue<string>();
            return new MyException(msg);
        }
    }


    [TestClass]
    public sealed class ExceptionTests
    {
        [TestMethod]
        public async Task EncodeAndDecodeException()
        {
            var converter = new ExceptionConverter();
            converter.RegisterException<MyException>(
                nameof(MyException), 
                (ex) => { return (MyException) ex; }, 
                (n) => { return (MyException) n; }
            );

            var sntEx = new MyException("Exception");
            JsonRpcException json_ex = converter.Encode(sntEx);
            Exception ex = converter.Decode(json_ex);
            Assert.Throws<MyException>(() => throw ex);

            MyException? rcvEx = ex as MyException;
            Assert.AreEqual(sntEx.Message, rcvEx?.Message);
        }

        [TestMethod]
        public async Task EncodeUnregisteredException()
        {
            var converter = new ExceptionConverter();

            var sntEx = new MyException("Exception");
            JsonRpcException json_ex = converter.Encode(sntEx);
            Assert.AreEqual(JsonRpcException.ErrorCode.exception_encoding_failed, json_ex.Code);
        }

        [TestMethod]
        public async Task DecodeUnregisteredException()
        {
            var converter1 = new ExceptionConverter();
            converter1.RegisterException<MyException>(
                nameof(MyException),
                (ex) => { return (MyException)ex; },
                (n) => { return (MyException)n; }
            );

            var sntEx = new MyException("Exception");
            JsonRpcException json_ex = converter1.Encode(sntEx);

            var converter2 = new ExceptionConverter();
            Exception ex = converter2.Decode(json_ex);

            Assert.Throws<JsonRpcException>(() => throw ex);
            Assert.AreEqual(JsonRpcException.ErrorCode.exception_decoding_failed, (ex as JsonRpcException).Code);
        }


        [TestMethod]
        public async Task PassesJsonRpcExceptions()
        {
            var converter = new ExceptionConverter();

            var sntEx = new JsonRpcException(JsonRpcException.ErrorCode.internal_error, "Exception");
            JsonRpcException json_ex = converter.Encode(sntEx);
            Exception rcvEx = converter.Decode(json_ex);

            Assert.Throws<JsonRpcException>(() => throw rcvEx);
        }

        [TestMethod]
        public async Task EncodingThrowsException()
        {
            var converter = new ExceptionConverter();
            converter.RegisterException<MyException>(
                nameof(MyException),
                (ex) => { throw new Exception(); },
                (n) => { return (MyException)n; }
            );

            var sntEx = new MyException("Exception");
            JsonRpcException json_ex = converter.Encode(sntEx);
            Exception rcvEx = converter.Decode(json_ex);

            Assert.Throws<JsonRpcException>(() => throw rcvEx);
        }
    }
}
