namespace DocStore.Core.Processing;

public interface IProcessingQueue
{
    ValueTask EnqueueAsync(Guid documentId, CancellationToken cancellationToken);
}
