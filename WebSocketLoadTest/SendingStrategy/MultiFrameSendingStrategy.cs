using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketEchoLoadTest.SendingStrategy
{
    public class MultiFrameSendingStrategy : SendingStragegy
    {
        readonly Int32 _frameLength;
        public MultiFrameSendingStrategy(Int32 frameLength, ArraySegment<Byte> message)
            :base(message)
        {
            _frameLength = frameLength;
        }
        public override async Task Send(ClientWebSocket ws, CancellationToken cancel)
        {
            Int32 cursor = Message.Offset;
            Int32 currentFrameLength;

            while(cursor <  Message.Count)
            {
                currentFrameLength = Math.Min(Message.Count - cursor, _frameLength);
                var frame = new ArraySegment<Byte>(Message.Array, cursor, currentFrameLength);

                cursor += currentFrameLength;
                await ws.SendAsync(frame, WebSocketMessageType.Text, cursor == Message.Count , cancel).ConfigureAwait(false);
            }
        }
    }
}
