WebSocket Echo Load Generator
=============================

This simple application simulates load on a target with parallel connections. Each connection performs echo operations in a loop, in other works, it sends a message and expects to have the same message back, and then repeat.

It is possible to configure:
 * The amount of connections.
 * The delay between echo operations, if any.
 * The message length.
 * The frame length. This setting, together with the message length, allows to send a message split across multiple frames.

Also, it supports `wss://`.

The application uses `System.Net.WebSockets.ClientWebSocket`, so Windows 8/2012 is required to run it.

### Usage

```
[Websocket URI] [ Client Amount ] [Message delay ms] [Message Byte Length] [Frame Byte Length]
```

For example, to simulate 1000 clients to `ws://myserver.com/api/ws` sending each messages of 512 bytes, and wait 100 milliseconds before starting the next echo operation it would be:

```
WebSocketEchoLoadGenerator ws://myserver.com/api/ws 1000 10 512 512
```

If you want to test the partial frames support, and want to send the message in two parts, indicate a smaller frame length:

```
WebSocketEchoLoadGenerator ws://myserver.com/api/ws 1000 100 512 256
```

If want to test maximum throughput just set the delay to 0:

```
WebSocketEchoLoadGenerator ws://myserver.com/api/ws 1000 0 512 256
```

Always keep an eye on your CPU usage and your network throughput, they may become the load generation bottleneck if you use high values.

### Performance Counters

The application has three performance counters under the "WebSocket Echo Load Test" category:

 * Echo Average Latency (seconds): The average latency in milliseconds of each echo operation.
 * Echoes per second: The amount of echo operations succesfully done per second. 
 * Connected: The number of connected clients.
 

