using DocStore.Core.Documents;
using Microsoft.EntityFrameworkCore;

namespace DocStore.Api.Infrastructure.Persistence;

public sealed class DocumentRepository(DocStoreDbContext db) : IDocumentRepository
{
    public Task<Document?> FindAsync(Guid documentId, CancellationToken cancellationToken) =>
        db.Documents.Include(d => d.Shares).FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

    public Task<Document?> FindByLinkTokenAsync(string linkToken, CancellationToken cancellationToken) =>
        db.Documents.Include(d => d.Shares).FirstOrDefaultAsync(d => d.ShareLinkToken == linkToken, cancellationToken);

    public async Task<IReadOnlyList<Guid>> ListPendingIdsAsync(CancellationToken cancellationToken) =>
        await db.Documents
            .Where(d => d.Status == ProcessingStatus.Pending)
            .OrderBy(d => d.UploadedAt)
            .Select(d => d.Id)
            .ToListAsync(cancellationToken);

    public void Add(Document document) => db.Documents.Add(document);

    public void Remove(Document document) => db.Documents.Remove(document);
}
