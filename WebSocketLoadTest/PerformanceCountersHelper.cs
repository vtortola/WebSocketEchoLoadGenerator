using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketEchoLoadGenerator
{
    public class PerformanceCountersHelper
    {
        static public PerformanceCounter EchoLatency, EchoLatencyBase, Connected, EchoCount;
        static String echoLatency = "Echo Average Latency (seconds)", echoCount="Echoes per second", connected="Connected";

        public static bool CreatePerformanceCounters()
        {
            string categoryName = "WebSocket Echo Load Test";

            if (!PerformanceCounterCategory.Exists(categoryName))
            {
                var ccdc = new CounterCreationDataCollection();

                ccdc.Add(new CounterCreationData
                {
                    CounterType = PerformanceCounterType.AverageTimer32,
                    CounterName = echoLatency
                });

                ccdc.Add(new CounterCreationData
                {
                    CounterType = PerformanceCounterType.AverageBase,
                    CounterName = echoLatency + "Base"
                });

                ccdc.Add(new CounterCreationData
                {
                    CounterType = PerformanceCounterType.RateOfCountsPerSecond64,
                    CounterName = echoCount
                });

                ccdc.Add(new CounterCreationData
                {
                    CounterType = PerformanceCounterType.NumberOfItems64,
                    CounterName = connected
                });

                PerformanceCounterCategory.Create(categoryName, "", PerformanceCounterCategoryType.Unknown, ccdc);
                return true;
            }
            else
            {
                //PerformanceCounterCategory.Delete(categoryName);
                //return true;

                EchoLatency = new PerformanceCounter(categoryName, echoLatency, false);
                EchoLatencyBase = new PerformanceCounter(categoryName, echoLatency + "Base", false);
                Connected = new PerformanceCounter(categoryName, connected, false);
                EchoCount = new PerformanceCounter(categoryName, echoCount, false);
                
                return false;
            }
        }

    }
}
