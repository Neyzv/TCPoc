using System.Diagnostics.CodeAnalysis;

namespace TCPoc.Shared.Framing;

public interface IMessageDecoder<TMessage>
{
    /// <summary>
    /// Try to decode a message from the <paramref name="buffer"/>, slice the buffer with only 
    /// unconsummed bytes and out the correct <typeparamref name="TMessage"/> instance.
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="message"></param>
    /// <returns>A bool indicating if a message have been decoded.</returns>
    bool TryDecodeMessage(ref Memory<byte> buffer, [NotNullWhen(true)] out TMessage? message);
}