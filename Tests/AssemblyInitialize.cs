using JsonRpc;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tests
{
    [TestClass]
    public static class Tests
    {
        [AssemblyInitialize]
        public static void SetLoggingHandler(TestContext context)
        {
            Logging.LogHandler += (s) => { System.Diagnostics.Debug.WriteLine(s); };
        }
    }
}
