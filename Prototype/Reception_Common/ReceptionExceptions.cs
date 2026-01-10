using JsonRpc;

namespace Reception_Common
{
    public class ReceptionExceptions
    {
        public static void RegisterReceptionExceptions(ExceptionConverter a_exceptionConverter)
        {
            a_exceptionConverter.Register<OrderNotFoundException>(
                nameof(OrderNotFoundException),
                (ex) => { return ex; },
                (n) => { return n; }
            );
        }
    }
}
