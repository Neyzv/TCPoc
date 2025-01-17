using System.Net.Sockets;
using TCPoc.Shared.Dispatcher;
using TCPoc.Shared.Framing;
using TCPoc.Shared.Transport;

namespace TCPoc.Server.Transport;

public abstract class BaseSession<TMessage>
    : SocketListener<TMessage>
{
    public BaseSession(Socket socket,
        IMessageEncoder<TMessage> messageEncoder,
        IMessageDecoder<TMessage> messageDecoder,
        IMessageDispatcher<TMessage> messageDispatcher,
        CancellationToken ct)
        : base(socket, messageEncoder, messageDecoder, messageDispatcher,
            CancellationTokenSource.CreateLinkedTokenSource(ct))
    { }

    /// <summary>
    /// Simple wrapper to <see cref="SocketListener{TMessage}.ListenAsync"/> methods to be accessed by the server.
    /// </summary>
    /// <returns>The <see cref="SocketListener{TMessage}.ListenAsync"/> <see cref="Task"/></returns>
    internal Task StartListeningAsync() =>
        ListenAsync();
}