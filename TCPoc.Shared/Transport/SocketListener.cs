using System.Buffers;
using System.Net;
using System.Net.Sockets;
using TCPoc.Shared.Dispatcher;
using TCPoc.Shared.Framing;

namespace TCPoc.Shared.Transport;

public abstract class SocketListener<TMessage>
    : IAsyncDisposable
{
    private const ushort BufferSize = 8192;

    private readonly CancellationTokenSource _cts;
    private readonly IMessageEncoder<TMessage> _messageEncoder;
    private readonly IMessageDecoder<TMessage> _messageDecoder;
    private readonly IMessageDispatcher<TMessage> _messageDispatcher;

    private bool _disposed;

    protected readonly Socket _socket;

    /// <summary>
    /// Gets the endpoint of the connectection.
    /// </summary>
    private IPEndPoint EndPoint =>
        (IPEndPoint)_socket.RemoteEndPoint!;

    /// <summary>
    /// Gets the IP address of the connectection.
    /// </summary>
    public IPAddress Address =>
        EndPoint.Address;

    /// <summary>
    /// Gets the port of the connectection.
    /// </summary>
    public int Port =>
        EndPoint.Port;

    /// <summary>
    /// Gets the cancellation token associated with the connection.
    /// </summary>
    public CancellationToken CancellationToken =>
        _cts.Token;

    /// <summary>
    /// Gets a value indicating whether the connection is available.
    /// </summary>
    public bool IsConnected =>
        !_cts.IsCancellationRequested && _socket.Connected && !_disposed;

    public SocketListener(Socket socket,
        IMessageEncoder<TMessage> messageEncoder,
        IMessageDecoder<TMessage> messageDecoder,
        IMessageDispatcher<TMessage> messageDispatcher,
        CancellationTokenSource cts)
    {
        _socket = socket;
        _cts = cts;

        _messageEncoder = messageEncoder;
        _messageDecoder = messageDecoder;
        _messageDispatcher = messageDispatcher;
    }

    /// <summary>
    /// Start listening for message from connection.
    /// </summary>
    protected async Task ListenAsync()
    {
        try
        {
            while (IsConnected)
            {
                using var memoryOwner = MemoryPool<byte>.Shared.Rent(BufferSize);
                var buffer = memoryOwner.Memory;

                var bytesRead = await _socket.ReceiveAsync(buffer, _cts.Token);

                if (bytesRead is 0)
                    break;

                buffer = buffer[..bytesRead];

                try
                {
                    while (IsConnected && _messageDecoder.TryDecodeMessage(ref buffer, out var message))
                    {
                        await _messageDispatcher
                            .DispatchMessageAsync(this, message)
                            .ConfigureAwait(false);
                    }
                }
                catch (SocketException)
                {
                    break;
                }
            }
        }
        finally
        {
            await DisposeAsync()
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Send a <typeparamref name="TMessage"/> through the connection.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <exception cref="InvalidOperationException">Thrown if the connection have been broken or not initialized.</exception>
    public ValueTask<int> SendAsync(TMessage message)
    {
        if (!IsConnected)
            throw new InvalidOperationException("The session socket isn't connected.");

        var buffer = _messageEncoder.EncodeMessage(message);

        return _socket.SendAsync(buffer, SocketFlags.None, _cts.Token);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (!_cts.IsCancellationRequested)
            await _cts.CancelAsync();

        try
        {
            _socket.Shutdown(SocketShutdown.Both);
        }
        catch (SocketException)
        {
            // ignored
        }
        finally
        {
            _socket.Close();

            _socket.Dispose();
            _cts.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}