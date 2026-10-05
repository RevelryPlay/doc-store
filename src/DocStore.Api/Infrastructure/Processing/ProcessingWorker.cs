using DocStore.Core.Documents;
using DocStore.Core.Processing;

namespace DocStore.Api.Infrastructure.Processing;

public sealed partial class ProcessingWorker(
    ChannelProcessingQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<ProcessingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RequeuePendingAsync(stoppingToken);

        await foreach (var documentId in queue.Reader.ReadAllAsync(stoppingToken))
        {
            // A fresh scope per document gives each one its own DbContext.
            await using var scope = scopeFactory.CreateAsyncScope();
            try
            {
                await scope.ServiceProvider.GetRequiredService<ProcessDocument>().ExecuteAsync(documentId, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // An unhandled exception here would stop the host, so one bad document must not escape.
                LogWorkerFailure(ex, documentId);
            }
        }
    }

    private async Task RequeuePendingAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var pending = await scope.ServiceProvider.GetRequiredService<IDocumentRepository>().ListPendingIdsAsync(cancellationToken);
        foreach (var documentId in pending)
        {
            await queue.EnqueueAsync(documentId, cancellationToken);
        }

        if (pending.Count > 0)
        {
            LogRequeued(pending.Count);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Processing worker failed on document {DocumentId}")]
    private partial void LogWorkerFailure(Exception exception, Guid documentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Re-queued {Count} pending documents")]
    private partial void LogRequeued(int count);
}
