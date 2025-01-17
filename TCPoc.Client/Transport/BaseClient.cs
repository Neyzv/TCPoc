using System.Net;
using System.Net.Sockets;
using TCPoc.Shared.Dispatcher;
using TCPoc.Shared.Framing;
using TCPoc.Shared.Transport;

namespace TCPoc.Client.Transport;

public abstract class BaseClient<TMessage>
    : SocketListener<TMessage>
{
    private readonly IPEndPoint _ipEndPoint;

    public BaseClient(IPAddress ipAdress,
        int port,
        IMessageEncoder<TMessage> messageEncoder,
        IMessageDecoder<TMessage> messageDecoder,
        IMessageDispatcher<TMessage> messageDispatcher,
        bool noDelay = true,
        int receiveTimeout = 5_000,
        int sendTimeout = 5_000
        ) : base(new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = noDelay,
            ReceiveTimeout = receiveTimeout,
            SendTimeout = sendTimeout,
            DualMode = true,
            LingerState = new LingerOption(true, 0)
        },
        messageEncoder, messageDecoder,
        messageDispatcher, new CancellationTokenSource()) =>
        _ipEndPoint = new IPEndPoint(ipAdress, port);

    /// <summary>
    /// Connect to the server.
    /// </summary>
    public async Task ConnectAsync()
    {
        await _socket.ConnectAsync(_ipEndPoint, CancellationToken).ConfigureAwait(false);

        _ = ListenAsync().ContinueWith(_ => DisposeAsync());
    }
}