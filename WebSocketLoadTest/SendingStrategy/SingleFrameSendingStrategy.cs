using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketEchoLoadGenerator.SendingStrategy
{
    public class SingleFrameSendingStrategy : SendingStragegy
    {
        public SingleFrameSendingStrategy(ArraySegment<Byte> message)
            :base(message)
        {

        }
        public override Task Send(ClientWebSocket ws,  CancellationToken cancel)
        {
            return ws.SendAsync(Message, WebSocketMessageType.Text, true, cancel);
        }
    }
}
