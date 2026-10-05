using DocStore.Core.Auditing;
using DocStore.Core.Common;
using DocStore.Core.Users;

namespace DocStore.Core.Documents;

public sealed class DocumentSharing(
    IDocumentRepository documents,
    IUserRepository users,
    IAuditLog audit,
    IUnitOfWork unitOfWork)
{
    public async Task<Result> ShareAsync(Guid documentId, Guid ownerId, Guid targetUserId, CancellationToken cancellationToken)
    {
        var owned = await RequireOwnerAsync(documentId, ownerId, cancellationToken);
        if (!owned.IsSuccess)
        {
            return owned.Error;
        }

        if (!await users.ExistsAsync(targetUserId, cancellationToken))
        {
            return Error.Invalid("User not found.");
        }

        var shared = owned.Value.ShareWith(targetUserId);
        if (!shared.IsSuccess)
        {
            return shared;
        }

        return await CommitAsync(documentId, ownerId, AuditAction.Shared, targetUserId, cancellationToken);
    }

    public Task<Result> UnshareAsync(Guid documentId, Guid ownerId, Guid targetUserId, CancellationToken cancellationToken) =>
        ChangeAsync(documentId, ownerId, AuditAction.Unshared, targetUserId, d => d.Unshare(targetUserId), cancellationToken);

    public async Task<Result<string>> EnableLinkAsync(Guid documentId, Guid ownerId, CancellationToken cancellationToken)
    {
        string? token = null;
        var result = await ChangeAsync(
            documentId, ownerId, AuditAction.LinkEnabled, targetUserId: null, d => token = d.EnableLinkSharing(), cancellationToken);

        return result.IsSuccess ? token! : result.Error;
    }

    public Task<Result> DisableLinkAsync(Guid documentId, Guid ownerId, CancellationToken cancellationToken) =>
        ChangeAsync(
            documentId, ownerId, AuditAction.LinkDisabled, targetUserId: null, d => d.DisableLinkSharing(), cancellationToken);

    private async Task<Result> ChangeAsync(
        Guid documentId,
        Guid ownerId,
        AuditAction action,
        Guid? targetUserId,
        Action<Document> change,
        CancellationToken cancellationToken)
    {
        var owned = await RequireOwnerAsync(documentId, ownerId, cancellationToken);
        if (!owned.IsSuccess)
        {
            return owned.Error;
        }

        change(owned.Value);
        return await CommitAsync(documentId, ownerId, action, targetUserId, cancellationToken);
    }

    private async Task<Result<Document>> RequireOwnerAsync(Guid documentId, Guid ownerId, CancellationToken cancellationToken) =>
        DocumentAccess.RequireOwner(await documents.FindAsync(documentId, cancellationToken), ownerId);

    private async Task<Result> CommitAsync(
        Guid documentId,
        Guid ownerId,
        AuditAction action,
        Guid? targetUserId,
        CancellationToken cancellationToken)
    {
        audit.Record(documentId, ownerId, action, targetUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
