using DocStore.Core.Auditing;

namespace DocStore.Api.Infrastructure.Persistence;

public sealed class AuditLog(DocStoreDbContext db, TimeProvider time) : IAuditLog
{
    public void Record(Guid documentId, Guid userId, AuditAction action, Guid? targetUserId = null) =>
        db.AuditEntries.Add(new AuditEntry(documentId, userId, action, targetUserId, time.GetUtcNow()));
}
