using DocStore.Core.Common;
using DocStore.Core.Documents;
using DocStore.Core.Storage;
using Microsoft.Extensions.Logging;

namespace DocStore.Core.Processing;

public sealed partial class ProcessDocument(
    IDocumentRepository documents,
    IDocumentStorage storage,
    DocumentProcessors processors,
    IUnitOfWork unitOfWork,
    ILogger<ProcessDocument> logger)
{
    public async Task ExecuteAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await documents.FindAsync(documentId, cancellationToken);
        if (document is not { Status: ProcessingStatus.Pending })
        {
            return;
        }

        var processor = processors.For(document.ContentType);
        if (processor is null)
        {
            LogNoProcessor(documentId, document.ContentType);
            document.MarkFailed();
        }
        else
        {
            try
            {
                await using var content = await storage.OpenReadAsync(document.StorageKey, cancellationToken);
                document.MarkProcessed(await processor.ProcessAsync(content, cancellationToken));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Processors parse untrusted files, so any failure marks the document rather than crashing the worker.
                LogProcessingFailed(ex, documentId);
                document.MarkFailed();
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "No processor registered for document {DocumentId} ({ContentType})")]
    private partial void LogNoProcessor(Guid documentId, string contentType);

    [LoggerMessage(Level = LogLevel.Error, Message = "Processing failed for document {DocumentId}")]
    private partial void LogProcessingFailed(Exception exception, Guid documentId);
}
