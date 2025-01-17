using TCPoc.Shared.Transport;

namespace TCPoc.Shared.Dispatcher;

public interface IMessageDispatcher<TMessage>
{
    /// <summary>
    /// Dispatch the <paramref name="message"/> to be handled.
    /// </summary>
    /// <param name="session">The session which have received the message.</param>
    /// <param name="message">The received message.</param>
    /// <returns></returns>
    ValueTask DispatchMessageAsync(SocketListener<TMessage> session, TMessage message);
}