namespace DocStore.Core.Auditing;

public enum AuditAction
{
    Uploaded,
    Viewed,
    Downloaded,
    Shared,
    Unshared,
    LinkEnabled,
    LinkDisabled,
    Deleted,
}

// DocumentId is deliberately not a foreign key so the trail survives document deletion.
public sealed class AuditEntry
{
    public AuditEntry(Guid documentId, Guid userId, AuditAction action, Guid? targetUserId, DateTimeOffset occurredAt)
    {
        DocumentId = documentId;
        UserId = userId;
        Action = action;
        TargetUserId = targetUserId;
        OccurredAt = occurredAt;
    }

    public long Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public Guid UserId { get; private set; }
    public AuditAction Action { get; private set; }

    /// <summary>The user a share was granted to or revoked from.</summary>
    public Guid? TargetUserId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
}
