WebSocket Echo Load Generator
=============================

This simple application simulates load on a target with parallel connections. It sends a message and waits for a response before sending the next one.

### Usage

```
Usage: [Websocket URI] [ Client Amount ] [Message delay ms] [Message Byte Length] [Frame Byte Length]
```

For example, to simulate 100 clients to `ws://myserver.com/api/ws` sending each messages of 512 bytes, and wait 10 milliseconds when the echo is received before sending the next one it would be>

```
WebSocketEchoLoadGenerator ws://myserver.com/api/ws 100 10 512 512
```

If you want to test the partial frames support, and want to send the message in two parts, indicate a smaller frame length:

```
WebSocketEchoLoadGenerator ws://myserver.com/api/ws 100 10 512 256
```

### Performance Counters

The application has three performance counters under the "WebSocket Echo Load Test" category:

 * Echo Average Latency (seconds): The average latency in milliseconds of each echo operation.
 * Echoes per second: The amount of echo operations succesfully done per second. 
 * Connected: The number of connected clients.
 
An echo operation involves sending the message, wait for a response and fully read the response.
