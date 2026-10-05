using DocStore.Core.Auditing;
using DocStore.Core.Common;
using DocStore.Core.Processing;
using DocStore.Core.Storage;

namespace DocStore.Core.Documents;

public sealed class UploadOptions
{
    public long MaxSizeBytes { get; init; } = 20 * 1024 * 1024;
}

public sealed class UploadDocument(
    IDocumentRepository documents,
    IDocumentStorage storage,
    DocumentProcessors processors,
    IProcessingQueue queue,
    IAuditLog audit,
    IUnitOfWork unitOfWork,
    TimeProvider time,
    UploadOptions options)
{
    /// <param name="content">Must be seekable; the header is sniffed before the full content is stored.</param>
    public async Task<Result<Guid>> ExecuteAsync(Guid ownerId, string fileName, Stream content, CancellationToken cancellationToken)
    {
        if (content.Length == 0)
        {
            return Error.Invalid("The file is empty.");
        }

        if (content.Length > options.MaxSizeBytes)
        {
            return Error.Invalid($"Files must be {options.MaxSizeBytes / (1024 * 1024)} MB or smaller.");
        }

        // The client-supplied content type is ignored; the bytes decide.
        var header = new byte[DocumentProcessors.HeaderLength];
        var read = await content.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        var processor = processors.Detect(header.AsSpan(0, read));
        if (processor is null)
        {
            return Error.Invalid("Unsupported file type.");
        }

        content.Position = 0;
        var storageKey = await storage.SaveAsync(content, cancellationToken);
        var document = new Document(ownerId, fileName, processor.ContentType, content.Length, storageKey, time.GetUtcNow());
        documents.Add(document);
        audit.Record(document.Id, ownerId, AuditAction.Uploaded);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await storage.DeleteAsync(storageKey, CancellationToken.None);
            throw;
        }

        // The row is committed, so it must reach the queue even if the client has gone.
        await queue.EnqueueAsync(document.Id, CancellationToken.None);
        return document.Id;
    }
}
