
# TCPoc
An optimized TCP server / client in NET 8 with memory pooling and low latency socket usage.

## How to use
To begin you'll need to implement a `IMessageEncoder<TMessage>` which will convert your `TMessage` into its binary representation. \
\
Then you'll need to implement a `IMessageDecoder<TMessage>` which will do the opposit job of the `IMessageEncoder<TMessage>`. \
\
To finish you must implement the `IMessageDispatcher<TMessage>` who aims to dispatch the received message to the wright handler to achieve the appropriate action.
### Server
If you need to implement a server, you'll have to extends two classes :
- BaseSession
- BaseTcpServer 
#### BaseSession
```cs
public sealed class MyTcpSession
	: BaseSession<MyMessage>
{
	// Your code here
}
```
#### BaseTcpServer
```cs
public sealed class MyTcpServer
	: BaseTcpServer<MyTcpSession, MyMessage>
{
    protected override MyTcpSession CreateSession(Socket socket) =>
        new MyTcpSession(socket,
            _myCustomMessageEncoder,
            _myCustomMessageDecoder,
            _myCustomMessageDispatcher,
            CancellationToken);

    protected override ValueTask OnSessionConnectedAsync(TSession session)
    {
        Console.WriteLine($"Client '{session.Address}' connected.");
	    
        return ValueTask.CompletedTask;
    }
    
    protected override ValueTask OnSessionDisconnectedAsync(TSession session)
    {
        Console.WriteLine($"Client '{session.Address}' disconnected.");
	    
        return ValueTask.CompletedTask;
    }
}
```
### Client
You'll just need to extend the `BaseClient<TMessage>` class.
```cs
public sealed class MyTcpClient
	: BaseClient<MyMessage>
{
	// Your code here
}
```
Program.cs
```cs
using var client = new MyTcpClient(...);
await client.ConnectAsync().ConfigureAwait(false);
// Your logic here, ex :
// await client.SendAsync(new MyMessage("Hello World !")).ConfigureAwait(false);
```
