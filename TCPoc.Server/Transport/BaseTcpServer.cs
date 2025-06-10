using System.Net;
using System.Net.Sockets;

namespace TCPoc.Server.Transport;

public abstract class BaseTcpServer<TSession, TMessage>
    : IAsyncDisposable
    where TSession : BaseSession<TMessage>
{
    private readonly int _receiveTimeout;
    private readonly int _sendTimeout;
    private readonly bool _noDelay;
    private readonly IPEndPoint _ipEndPoint;
    private readonly Socket _socket;
    private readonly CancellationTokenSource _cts;

    private bool _disposed;

    /// <summary>
    /// Gets the IP address of the server.
    /// </summary>
    public IPAddress Address =>
        _ipEndPoint.Address;

    /// <summary>
    /// Gets the port of the server.
    /// </summary>
    public int Port =>
        _ipEndPoint.Port;

    /// <summary>
    /// The cancellation token of the server.
    /// </summary>
    public CancellationToken CancellationToken =>
        _cts.Token;

    /// <summary>
    /// Gets a value indicating whether the server is running.
    /// </summary>
    public bool IsRunning =>
        !_cts.IsCancellationRequested && _socket.IsBound && !_disposed;

    public BaseTcpServer(IPAddress ipAdress,
        int port,
        int receiveTimeout = 5_000,
        int sendTimeout = 5_000,
        bool noDelay = true)
    {
        _ipEndPoint = new IPEndPoint(ipAdress, port);
        _receiveTimeout = receiveTimeout;
        _sendTimeout = sendTimeout;
        _noDelay = noDelay;

        _socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp)
        {
            DualMode = true,
            LingerState = new LingerOption(true, 0)
        };
        _cts = new CancellationTokenSource();
    }

    /// <summary>
    /// Create a session based on the received socket connection.
    /// </summary>
    /// <param name="socket">The received socket connection.</param>
    /// <returns>An instance which represent the receiving session connection.</returns>
    protected abstract TSession CreateSession(Socket socket);

    /// <summary>
    /// Called when a connection have been received.
    /// </summary>
    /// <param name="session">The received connection session.</param>
    protected virtual ValueTask OnSessionConnectedAsync(TSession session)
    {
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Called when a session is finished and the user disconnected, before its ressources been disposed.
    /// </summary>
    /// <param name="session">The session which have been stopped.</param>
    protected virtual ValueTask OnSessionDisconnectedAsync(TSession session)
    {
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Configure received connection socket options.
    /// </summary>
    /// <param name="socket">The received connection socket.</param>
    private void ConfigureSocket(Socket socket)
    {
        socket.NoDelay = _noDelay;
        socket.ReceiveTimeout = _receiveTimeout;
        socket.SendTimeout = _sendTimeout;
    }

    /// <summary>
    /// Start the server, and allow it to receive connections.<br/>
    /// The task willn't stop until <see cref="DisposeAsync"/> is called.
    /// </summary>
    public async Task StartAsync()
    {
        try
        {
            _socket.Bind(_ipEndPoint);
            _socket.Listen();

            while (IsRunning)
            {
                var sessionSocket = await _socket
                    .AcceptAsync(_cts.Token)
                    .ConfigureAwait(false);

                ConfigureSocket(sessionSocket);

                var session = CreateSession(sessionSocket);

                await OnSessionConnectedAsync(session)
                    .ConfigureAwait(false);

                _ = session
                    .StartListeningAsync()
                    .ContinueWith(_ => OnSessionDisconnectedAsync(session), _cts.Token)
                    .ContinueWith(_ => session.DisposeAsync());
            }
        }
        finally
        {
            await DisposeAsync().ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (!_cts.IsCancellationRequested)
            await _cts
                .CancelAsync()
                .ConfigureAwait(false);

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
