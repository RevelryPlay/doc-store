using DocStore.Core.Auditing;
using DocStore.Core.Common;
using DocStore.Core.Storage;

namespace DocStore.Core.Documents;

public sealed class DeleteDocument(
    IDocumentRepository documents,
    IDocumentStorage storage,
    IAuditLog audit,
    IUnitOfWork unitOfWork)
{
    public async Task<Result> ExecuteAsync(Guid documentId, Guid ownerId, CancellationToken cancellationToken)
    {
        var owned = DocumentAccess.RequireOwner(await documents.FindAsync(documentId, cancellationToken), ownerId);
        if (!owned.IsSuccess)
        {
            return owned.Error;
        }

        documents.Remove(owned.Value);
        audit.Record(documentId, ownerId, AuditAction.Deleted);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // The database is the source of truth, so content is removed only after the row is gone.
        await storage.DeleteAsync(owned.Value.StorageKey, cancellationToken);
        return Result.Success();
    }
}
