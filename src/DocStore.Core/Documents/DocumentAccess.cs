using System.Security.Cryptography;
using System.Text;
using DocStore.Core.Common;

namespace DocStore.Core.Documents;

public enum AccessLevel
{
    None,
    LinkHolder,
    SharedUser,
    Owner,
}

public static class DocumentAccess
{
    public static AccessLevel Evaluate(Document document, Guid userId, string? linkToken)
    {
        if (document.OwnerId == userId)
        {
            return AccessLevel.Owner;
        }

        if (document.IsSharedWith(userId))
        {
            return AccessLevel.SharedUser;
        }

        return TokenMatches(document.ShareLinkToken, linkToken) ? AccessLevel.LinkHolder : AccessLevel.None;
    }

    // Callers without access get NotFound rather than Forbidden so a document's existence never leaks.
    public static Result<Document> RequireAccess(Document? document, Guid userId, string? linkToken) =>
        document is not null && Evaluate(document, userId, linkToken) != AccessLevel.None
            ? document
            : NotFound;

    public static Result<Document> RequireOwner(Document? document, Guid userId)
    {
        if (document is null)
        {
            return NotFound;
        }

        return Evaluate(document, userId, linkToken: null) switch
        {
            AccessLevel.Owner => document,
            AccessLevel.None => NotFound,
            _ => Error.Forbidden("Only the owner can change this document."),
        };
    }

    private static Error NotFound => Error.NotFound("Document not found.");

    private static bool TokenMatches(string? expected, string? presented) =>
        expected is not null
        && presented is not null
        && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(presented));
}
