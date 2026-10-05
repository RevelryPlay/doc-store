using System.Buffers.Text;
using System.Security.Cryptography;
using DocStore.Core.Common;
using DocStore.Core.Processing;

namespace DocStore.Core.Documents;

public enum ProcessingStatus
{
    Pending,
    Ready,
    Failed,
}

public sealed class Document
{
    public const int MaxFileNameLength = 255;
    public const int MaxPreviewLength = 1000;
    public const int MaxMetadataLength = 255;

    private readonly List<DocumentShare> _shares = [];

    public Document(Guid ownerId, string fileName, string contentType, long sizeBytes, string storageKey, DateTimeOffset uploadedAt)
    {
        Id = Guid.CreateVersion7();
        OwnerId = ownerId;
        FileName = NormalizeFileName(fileName);
        ContentType = contentType;
        SizeBytes = sizeBytes;
        StorageKey = storageKey;
        UploadedAt = uploadedAt;
        Status = ProcessingStatus.Pending;
    }

    public Guid Id { get; private set; }
    public Guid OwnerId { get; private set; }
    public string FileName { get; private set; }
    public string ContentType { get; private set; }
    public long SizeBytes { get; private set; }
    public string StorageKey { get; private set; }
    public DateTimeOffset UploadedAt { get; private set; }
    public ProcessingStatus Status { get; private set; }
    public int? PageCount { get; private set; }
    public string? Title { get; private set; }
    public string? Author { get; private set; }
    public string? TextPreview { get; private set; }
    public string? ShareLinkToken { get; private set; }
    public IReadOnlyCollection<DocumentShare> Shares => _shares;

    public Result ShareWith(Guid userId)
    {
        if (userId == OwnerId)
        {
            return Error.Invalid("A document cannot be shared with its owner.");
        }

        if (!IsSharedWith(userId))
        {
            _shares.Add(new DocumentShare(Id, userId));
        }

        return Result.Success();
    }

    public void Unshare(Guid userId) => _shares.RemoveAll(s => s.UserId == userId);

    public bool IsSharedWith(Guid userId) => _shares.Exists(s => s.UserId == userId);

    public string EnableLinkSharing() =>
        ShareLinkToken ??= Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

    public void DisableLinkSharing() => ShareLinkToken = null;

    public Result MarkProcessed(ProcessingResult result)
    {
        if (Status != ProcessingStatus.Pending)
        {
            return Error.Invalid($"Document is already {Status}.");
        }

        PageCount = result.PageCount;
        Title = Truncate(result.Title, MaxMetadataLength);
        Author = Truncate(result.Author, MaxMetadataLength);
        TextPreview = Truncate(result.TextPreview, MaxPreviewLength);
        Status = ProcessingStatus.Ready;
        return Result.Success();
    }

    public void MarkFailed() => Status = ProcessingStatus.Failed;

    // The name is display-only; storage keys never derive from it.
    private static string NormalizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName.Replace('\\', '/')).Trim();
        return name.Length == 0 ? "document" : Truncate(name, MaxFileNameLength)!;
    }

    private static string? Truncate(string? value, int maxLength) =>
        value is null || value.Length <= maxLength ? value : value[..maxLength];
}

public sealed class DocumentShare
{
    public DocumentShare(Guid documentId, Guid userId)
    {
        DocumentId = documentId;
        UserId = userId;
    }

    public Guid DocumentId { get; private set; }
    public Guid UserId { get; private set; }
}
