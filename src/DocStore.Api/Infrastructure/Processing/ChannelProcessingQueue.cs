using System.Threading.Channels;
using DocStore.Core.Processing;

namespace DocStore.Api.Infrastructure.Processing;

/// <summary>
/// In-process queue. Durability comes from the database: pending rows are re-queued on startup.
/// A multi-instance deployment would replace this with a durable workflow engine.
/// </summary>
public sealed class ChannelProcessingQueue : IProcessingQueue
{
    // Unbounded because the worker re-queues pending rows before it starts reading; entries are just ids.
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions { SingleReader = true });

    public ChannelReader<Guid> Reader => _channel.Reader;

    public ValueTask EnqueueAsync(Guid documentId, CancellationToken cancellationToken) =>
        _channel.Writer.WriteAsync(documentId, cancellationToken);
}
