namespace DocStore.Core.Auditing;

/// <summary>Records are committed with the caller's <see cref="Common.IUnitOfWork"/>.</summary>
public interface IAuditLog
{
    void Record(Guid documentId, Guid userId, AuditAction action, Guid? targetUserId = null);
}
