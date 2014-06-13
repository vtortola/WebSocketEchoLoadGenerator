using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketEchoLoadGenerator.SendingStrategy;

namespace WebSocketEchoLoadGenerator
{
    class Program
    {
        static Int32 _amount, _frameLength, _interval, _messageLength;
        static Uri _host;
        static String _message = Guid.NewGuid().ToString();

        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.White;
            Console.WriteLine("                            ");
            Console.WriteLine("  WebSocket load generator  ");
            Console.WriteLine("                            ");
            Console.WriteLine();
            Console.ResetColor();

            if (PerformanceCountersHelper.CreatePerformanceCounters())
            {
                Console.WriteLine("Performance counters have been created, please re-run the app");
                Console.ReadKey(true);
                return;
            }

            if (args == null || args.Length < 5 || 
                String.IsNullOrWhiteSpace(args[0]) || !Uri.TryCreate(args[0], UriKind.Absolute, out _host) || 
                !Int32.TryParse(args[1], out _amount) ||
                !Int32.TryParse(args[2], out _interval) ||
                !Int32.TryParse(args[3], out _messageLength) ||
                !Int32.TryParse(args[4], out _frameLength))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Usage: [Websocket URI] [ Client Amount ] [Message delay ms] [Message Byte Length] [Frame Byte Length]");
                Console.ResetColor();
                Console.ReadKey(true);
                return;
            }

            // Accept any SSL/TLS certificate
            System.Net.ServicePointManager.ServerCertificateValidationCallback = (a,b,c,d)=> true;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(" * Clients: " + _amount.ToString() + ", message delay: " + _interval.ToString() + " ms.");
            Console.WriteLine(" * Message size: " + _messageLength.ToString() + " bytes, frame size: " + _frameLength.ToString() +" bytes.");
            Console.WriteLine(" * Each message will take " + (_messageLength / _frameLength).ToString() + " frames.");
            Console.ResetColor();

            Console.WriteLine();
            Console.WriteLine("Connecting 50 clients each 500 ms.");

            while (_messageLength > _message.Length)
                _message += _message;
            _message = _message.Substring(0, _messageLength);

            Byte[] msgOut = Encoding.UTF8.GetBytes(_message);
            ArraySegment<Byte> segmentOut = new ArraySegment<Byte>(msgOut, 0, msgOut.Length);

            SendingStragegy sendingStrategy = _messageLength == _frameLength ?
                (SendingStragegy)new SingleFrameSendingStrategy(segmentOut) :
                (SendingStragegy)new MultiFrameSendingStrategy(_frameLength,segmentOut);
            
            CancellationTokenSource cancelSource = new CancellationTokenSource();

            Console.WriteLine("Press any key to stop the test...\n");
            Random ran = new Random(DateTime.Now.Millisecond);
            List<Task> list = new List<Task>();
            try
            {
                for (int i = 0; i < _amount; i++)
			    {
                    list.Add(Task.Run(()=> StartClient(ran, cancelSource.Token, sendingStrategy)));
                    if(i%50==0)
                        Thread.Sleep(500);
                }
            }
            catch(AggregateException aex)
            {
                var ex = aex.GetBaseException();
                while (ex.InnerException != null)
                    ex = ex.InnerException.GetBaseException();
                throw ex;
            }

            Console.ReadKey(true);
            cancelSource.Cancel();
            Console.WriteLine("[Waiting for all clients to close]");
            Task.WhenAll(list).Wait();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("End");
            Console.ReadKey(true);
        }

        private static async Task StartClient(Random ran, CancellationToken cancel, SendingStragegy strategy)
        {
            Byte[] msgIn = new Byte[4096];
            ArraySegment<Byte> segmentIn = new ArraySegment<Byte>(msgIn, 0, msgIn.Length);

            while (!cancel.IsCancellationRequested)
            {
                await Task.Delay(ran.Next(0, 450), cancel).ConfigureAwait(false);

                try
                {
                    ClientWebSocket client = new ClientWebSocket();
                    await client.ConnectAsync(_host, cancel).ConfigureAwait(false);

                    Console.Write("·");
                    PerformanceCountersHelper.Connected.Increment();
                    Stopwatch stopwatch = new Stopwatch();
                    
                    while (!cancel.IsCancellationRequested)
                    {
                        stopwatch.Start();
                        await strategy.Send(client, cancel).ConfigureAwait(false);

                        String s = null;
                        using (var ms = new MemoryStream())
                        {
                            WebSocketReceiveResult result;
                            do
                            {
                                result = await client.ReceiveAsync(segmentIn, cancel).ConfigureAwait(false);
                                ms.Write(segmentIn.Array, segmentIn.Offset, result.Count);
                            }
                            while (!result.EndOfMessage);

                            stopwatch.Stop();

                            var array = ms.ToArray();
                            s = Encoding.UTF8.GetString(array, 0, array.Length);
                        }

                        if (!String.IsNullOrEmpty(s) && !s.Equals(_message, StringComparison.Ordinal))
                            throw new Exception("Response is different from request, sent: " + _frameLength + ", received: " + (s == null ? 0 : s.Length).ToString());

                        PerformanceCountersHelper.EchoLatency.IncrementBy(stopwatch.ElapsedTicks);
                        PerformanceCountersHelper.EchoLatencyBase.IncrementBy(1);
                        PerformanceCountersHelper.EchoCount.Increment();

                        if (_interval != 0)
                            await Task.Delay(_interval, cancel).ConfigureAwait(false);

                        stopwatch.Reset();
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (AggregateException aex)
                {
                    if (!cancel.IsCancellationRequested)
                    {
                        var ex = aex.GetBaseException();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(DateTime.Now.ToString("dd/MM/yyy hh:mm:ss.fff ") + "(" + ex.GetType().Name + ") " + ex.Message);
                        Console.ResetColor();
                        Thread.Sleep(2000);
                    }
                }
                catch (Exception ex)
                {
                    if (!cancel.IsCancellationRequested)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(DateTime.Now.ToString("dd/MM/yyy hh:mm:ss.fff ") + "(" + ex.GetType().Name + ") " + ex.Message);
                        Console.ResetColor();
                        Thread.Sleep(2000);
                    }
                }
                finally
                {
                    PerformanceCountersHelper.Connected.Decrement();
                    Console.Write("~");
                }
            }

        }
    }
}
