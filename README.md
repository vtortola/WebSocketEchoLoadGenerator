WebSocket Echo Load Generator
=============================

This simple application simulates load on a target with parallel connections. It sends a message and waits for a response before sending the next one.

An echo operation involves sending the message, wait for a response and fully read the response.

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
 

