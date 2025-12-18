using System;
using System.Collections.Generic;
using System.Text;

namespace JsonRpc
{
    public interface IClientConnector
    {
        public string Send(string request);
    }
}
