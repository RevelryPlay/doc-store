using Dapper;
using DocStore.Core.Auditing;
using DocStore.Core.Documents;
using Microsoft.EntityFrameworkCore;

namespace DocStore.Api.Infrastructure.Persistence;

/// <summary>
/// Read-side queries as hand-written SQL. The library view is the hottest read path, and the activity feed
/// joins audit rows to user names, which an EF projection would make no clearer.
/// They share the request's EF connection instead of opening a second one.
/// </summary>
public sealed class DocumentQueries(DocStoreDbContext db) : IDocumentQueries
{
    private const string LibrarySql = """
        SELECT d.Id, d.FileName, d.SizeBytes, d.UploadedAt, d.Status, d.PageCount,
               u.DisplayName AS OwnerName,
               (SELECT COUNT(*) FROM DocumentShares s WHERE s.DocumentId = d.Id) AS ShareCount,
               CAST(IIF(d.ShareLinkToken IS NULL, 0, 1) AS bit) AS IsLinkShared,
               CAST(IIF(d.OwnerId = @UserId, 1, 0) AS bit) AS IsOwned
        FROM Documents d
        JOIN Users u ON u.Id = d.OwnerId
        WHERE d.OwnerId = @UserId
           OR EXISTS (SELECT 1 FROM DocumentShares s WHERE s.DocumentId = d.Id AND s.UserId = @UserId)
        ORDER BY d.UploadedAt DESC
        """;

    private const string ActivitySql = """
        SELECT TOP (@Limit) a.Action, a.OccurredAt, actor.DisplayName AS ActorName, target.DisplayName AS TargetName
        FROM AuditEntries a
        JOIN Users actor ON actor.Id = a.UserId
        LEFT JOIN Users target ON target.Id = a.TargetUserId
        WHERE a.DocumentId = @DocumentId
        ORDER BY a.OccurredAt DESC, a.Id DESC
        """;

    public async Task<IReadOnlyList<DocumentActivity>> GetActivityAsync(Guid documentId, int limit, CancellationToken cancellationToken)
    {
        var rows = await db.Database.GetDbConnection().QueryAsync<ActivityRow>(
            new CommandDefinition(ActivitySql, new { DocumentId = documentId, Limit = limit }, cancellationToken: cancellationToken));

        return [.. rows.Select(row => row.ToActivity())];
    }

    public async Task<DocumentLibrary> GetLibraryAsync(Guid userId, CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        var rows = await connection.QueryAsync<LibraryRow>(
            new CommandDefinition(LibrarySql, new { UserId = userId }, cancellationToken: cancellationToken));

        var owned = new List<DocumentListItem>();
        var shared = new List<DocumentListItem>();
        foreach (var row in rows)
        {
            (row.IsOwned ? owned : shared).Add(row.ToItem());
        }

        return new DocumentLibrary(owned, shared);
    }

    private sealed class ActivityRow
    {
        public string Action { get; init; } = "";
        public DateTimeOffset OccurredAt { get; init; }
        public string ActorName { get; init; } = "";
        public string? TargetName { get; init; }

        public DocumentActivity ToActivity() => new(Enum.Parse<AuditAction>(Action), ActorName, TargetName, OccurredAt);
    }

    private sealed class LibraryRow
    {
        public Guid Id { get; init; }
        public string FileName { get; init; } = "";
        public long SizeBytes { get; init; }
        public DateTimeOffset UploadedAt { get; init; }
        public string Status { get; init; } = "";
        public int? PageCount { get; init; }
        public string OwnerName { get; init; } = "";
        public int ShareCount { get; init; }
        public bool IsLinkShared { get; init; }
        public bool IsOwned { get; init; }

        public DocumentListItem ToItem() => new(
            Id, FileName, SizeBytes, UploadedAt, Enum.Parse<ProcessingStatus>(Status), PageCount, OwnerName, ShareCount, IsLinkShared);
    }
}
