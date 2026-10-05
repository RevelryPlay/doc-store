namespace DocStore.Core.Documents;

public interface IDocumentRepository
{
    Task<Document?> FindAsync(Guid documentId, CancellationToken cancellationToken);
    Task<Document?> FindByLinkTokenAsync(string linkToken, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> ListPendingIdsAsync(CancellationToken cancellationToken);
    void Add(Document document);
    void Remove(Document document);
}
