namespace TCPoc.Shared.Framing;

public interface IMessageEncoder<TMessage>
{
    /// <summary>
    /// Encode an intance of <typeparamref name="TMessage"/> into a binary representation.
    /// </summary>
    /// <param name="message">The instance of <typeparamref name="TMessage"/> to encode.</param>
    /// <returns>The binary representation of the message.</returns>
    ReadOnlyMemory<byte> EncodeMessage(TMessage message);
}