using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketEchoLoadGenerator.SendingStrategy;

namespace WebSocketEchoLoadGenerator
{
    class Program
    {
        static Int32 _amount, _frameLength, _delay, _messageLength;
        static Uri _host;
        static String _message = Guid.NewGuid().ToString();
        static CancellationTokenSource _cancellation = new CancellationTokenSource();

        static void Main(string[] args)
        {
            // Log unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += (o, e) => ShowException(e.ExceptionObject as Exception);

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

            if(!TryParseCommandLineParameters(args))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Usage: [Websocket URI] [ Client Amount ] [Message delay ms] [Message Byte Length] [Frame Byte Length]");
                Console.ResetColor();
                Console.ReadKey(true);
                return;
            }

            if (_frameLength > _messageLength)
                throw new ArgumentException("Message length cannot be bigger than frame length.");
            
            // Accept any SSL/TLS certificate
            System.Net.ServicePointManager.ServerCertificateValidationCallback = (a,b,c,d)=> true;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(" * Clients: " + _amount.ToString() + ", message delay: " + _delay.ToString() + " ms.");
            Console.WriteLine(" * Message size: " + _messageLength.ToString() + " bytes, frame size: " + _frameLength.ToString() +" bytes.");
            Console.WriteLine(" * Each message will take " + (_messageLength / _frameLength).ToString() + " frames.");
            Console.WriteLine(" * Connecting 50 clients each 500 ms.");
            Console.ResetColor();

            Console.WriteLine("\nPress any key to stop the test...\n");
            List<Task> list = StartClients();

            Console.ReadKey(true);
            _cancellation.Cancel();
            Console.WriteLine("\n[Waiting for all clients to close]");
            Task.WhenAll(list).Wait();
            Console.WriteLine("\n\nEnd");
            Console.ReadKey(true);
        }

        private static List<Task> StartClients()
        {
            Byte[] msgOut = Encoding.UTF8.GetBytes(_message);
            ArraySegment<Byte> segmentOut = new ArraySegment<Byte>(msgOut, 0, msgOut.Length);

            SendingStragegy sendingStrategy = _messageLength == _frameLength ?
                (SendingStragegy)new SingleFrameSendingStrategy(segmentOut) :
                (SendingStragegy)new MultiFrameSendingStrategy(_frameLength, segmentOut);

            Random ran = new Random(DateTime.Now.Millisecond);

            return Enumerable.Range(0, _amount)
                             .Select(i => Task.Run(() => StartClient(ran, _cancellation.Token, sendingStrategy)))
                             .ToList();
        }

        private static bool TryParseCommandLineParameters(String[] args)
        {
            if (args == null || args.Length < 5 ||
                String.IsNullOrWhiteSpace(args[0]) || !Uri.TryCreate(args[0], UriKind.Absolute, out _host) ||
                !Int32.TryParse(args[1], out _amount) ||
                !Int32.TryParse(args[2], out _delay) ||
                !Int32.TryParse(args[3], out _messageLength) ||
                !Int32.TryParse(args[4], out _frameLength))
            {
                return false;
            }

            while (_messageLength > _message.Length)
                _message += _message;
            _message = _message.Substring(0, _messageLength);

            return true;
        }

        private static async Task StartClient(Random ran, CancellationToken cancel, SendingStragegy strategy)
        {
            Byte[] msgIn = new Byte[4096];
            ArraySegment<Byte> segmentIn = new ArraySegment<Byte>(msgIn, 0, msgIn.Length);

            while (!cancel.IsCancellationRequested)
            {
                await Task.Delay(ran.Next(0, 450), cancel).ConfigureAwait(false);

                ClientWebSocket client = new ClientWebSocket();
                try
                {
                    await client.ConnectAsync(_host, cancel).ConfigureAwait(false);

                    Console.Write("·");
                    PerformanceCountersHelper.Connected.Increment();
                    Stopwatch stopwatch = new Stopwatch();
                    
                    while (!cancel.IsCancellationRequested)
                    {
                        stopwatch.Start();
                        await strategy.Send(client, cancel).ConfigureAwait(false);

                        String message = null;
                        WebSocketReceiveResult result = await client.ReceiveAsync(segmentIn, cancel).ConfigureAwait(false);

                        // it could be more compact, but this way avoids creating the stream until the await returns with an actual response
                        using (var ms = new MemoryStream())
                        {
                            ms.Write(segmentIn.Array, segmentIn.Offset, result.Count);
   
                            while (!result.EndOfMessage) 
                            {
                                result = await client.ReceiveAsync(segmentIn, cancel).ConfigureAwait(false);
                                ms.Write(segmentIn.Array, segmentIn.Offset, result.Count);
                            }

                            stopwatch.Stop();

                            var array = ms.ToArray();
                            message = Encoding.UTF8.GetString(array, 0, array.Length);
                        }

                        if (!String.IsNullOrEmpty(message) && !message.Equals(_message, StringComparison.Ordinal))
                            throw new Exception("Response is different from request, sent: " + _frameLength + ", received: " + (message == null ? 0 : message.Length).ToString());

                        PerformanceCountersHelper.EchoLatency.IncrementBy(stopwatch.ElapsedTicks);
                        PerformanceCountersHelper.EchoLatencyBase.IncrementBy(1);
                        PerformanceCountersHelper.EchoCount.Increment();

                        if (_delay != 0)
                            await Task.Delay(_delay, cancel).ConfigureAwait(false);

                        stopwatch.Reset();
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    ShowException(ex);
                    _cancellation.Cancel();
                }
                finally
                {
                    PerformanceCountersHelper.Connected.Decrement();
                    Console.Write("~");
                    client.Dispose();
                }
            }

        }

        private static void ShowException(Exception ex)
        {
            if (ex == null)
                ex = new Exception("[ShowException] was called empty.");

            var aex = ex as AggregateException;
            if (aex != null)
                ex = aex.GetBaseException();

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(DateTime.Now.ToString("dd/MM/yyy hh:mm:ss.fff ") + "(" + ex.GetType().Name + ") " + ex.Message);
            Console.ResetColor();
            Thread.Sleep(2000);
        }
    }
}
