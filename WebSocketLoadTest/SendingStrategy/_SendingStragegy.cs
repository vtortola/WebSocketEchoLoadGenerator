using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketEchoLoadTest.SendingStrategy
{
    public abstract class SendingStragegy
    {
        protected readonly ArraySegment<Byte> Message;
        public SendingStragegy(ArraySegment<Byte> message)
        {
            Message = message;
        }

        public abstract Task Send(ClientWebSocket ws, CancellationToken cancel);
    }
}
