using DocStore.Core.Auditing;

namespace DocStore.Core.Documents;

/// <summary>Read side for list views. Link-only access never adds a document to a user's library.</summary>
public interface IDocumentQueries
{
    Task<DocumentLibrary> GetLibraryAsync(Guid userId, CancellationToken cancellationToken);

    /// <returns>Most recent first.</returns>
    Task<IReadOnlyList<DocumentActivity>> GetActivityAsync(Guid documentId, int limit, CancellationToken cancellationToken);
}

public sealed record DocumentLibrary(IReadOnlyList<DocumentListItem> Owned, IReadOnlyList<DocumentListItem> SharedWithMe);

public sealed record DocumentActivity(AuditAction Action, string ActorName, string? TargetName, DateTimeOffset OccurredAt);

public sealed record DocumentListItem(
    Guid Id,
    string FileName,
    long SizeBytes,
    DateTimeOffset UploadedAt,
    ProcessingStatus Status,
    int? PageCount,
    string OwnerName,
    int ShareCount,
    bool IsLinkShared);
