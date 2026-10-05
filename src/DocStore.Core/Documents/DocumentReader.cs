using DocStore.Core.Auditing;
using DocStore.Core.Common;
using DocStore.Core.Storage;

namespace DocStore.Core.Documents;

public sealed record DocumentDetails(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset UploadedAt,
    ProcessingStatus Status,
    int? PageCount,
    string? Title,
    string? Author,
    string? TextPreview,
    AccessLevel Access,
    string? ShareLinkToken,
    IReadOnlyList<Guid> SharedWith)
{
    // Sharing state is only exposed to the owner.
    public static DocumentDetails From(Document document, AccessLevel access)
    {
        var isOwner = access == AccessLevel.Owner;
        return new(
            document.Id,
            document.FileName,
            document.ContentType,
            document.SizeBytes,
            document.UploadedAt,
            document.Status,
            document.PageCount,
            document.Title,
            document.Author,
            document.TextPreview,
            access,
            isOwner ? document.ShareLinkToken : null,
            isOwner ? [.. document.Shares.Select(s => s.UserId)] : []);
    }
}

public sealed record DocumentContent(Stream Content, string FileName, string ContentType);

public sealed class DocumentReader(
    IDocumentRepository documents,
    IDocumentStorage storage,
    IAuditLog audit,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<DocumentDetails>> GetAsync(Guid documentId, Guid userId, string? linkToken, CancellationToken cancellationToken)
    {
        var document = await documents.FindAsync(documentId, cancellationToken);
        return ToDetails(document, userId, linkToken);
    }

    public async Task<Result<DocumentDetails>> GetByLinkAsync(string linkToken, Guid userId, CancellationToken cancellationToken)
    {
        var document = await documents.FindByLinkTokenAsync(linkToken, cancellationToken);
        return ToDetails(document, userId, linkToken);
    }

    public async Task<Result<DocumentContent>> OpenAsync(Guid documentId, Guid userId, string? linkToken, CancellationToken cancellationToken)
    {
        var access = DocumentAccess.RequireAccess(await documents.FindAsync(documentId, cancellationToken), userId, linkToken);
        if (!access.IsSuccess)
        {
            return access.Error;
        }

        var document = access.Value;
        audit.Record(document.Id, userId, AuditAction.Downloaded);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var content = await storage.OpenReadAsync(document.StorageKey, cancellationToken);
        return new DocumentContent(content, document.FileName, document.ContentType);
    }

    private static Result<DocumentDetails> ToDetails(Document? document, Guid userId, string? linkToken)
    {
        var access = DocumentAccess.RequireAccess(document, userId, linkToken);
        return access.IsSuccess
            ? DocumentDetails.From(access.Value, DocumentAccess.Evaluate(access.Value, userId, linkToken))
            : access.Error;
    }
}
