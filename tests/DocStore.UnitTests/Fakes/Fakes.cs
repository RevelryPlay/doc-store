using System.Text;
using DocStore.Core.Auditing;
using DocStore.Core.Common;
using DocStore.Core.Documents;
using DocStore.Core.Processing;
using DocStore.Core.Storage;
using DocStore.Core.Users;

namespace DocStore.UnitTests.Fakes;

internal sealed class InMemoryDocuments : IDocumentRepository, IUnitOfWork
{
    public List<Document> Items { get; } = [];
    public int SaveCount { get; private set; }
    public Exception? FailNextSave { get; set; }

    public Task<Document?> FindAsync(Guid documentId, CancellationToken cancellationToken) =>
        Task.FromResult(Items.Find(d => d.Id == documentId));

    public Task<Document?> FindByLinkTokenAsync(string linkToken, CancellationToken cancellationToken) =>
        Task.FromResult(Items.Find(d => d.ShareLinkToken == linkToken));

    public Task<IReadOnlyList<Guid>> ListPendingIdsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Guid>>([.. Items.Where(d => d.Status == ProcessingStatus.Pending).Select(d => d.Id)]);

    public void Add(Document document) => Items.Add(document);

    public void Remove(Document document) => Items.Remove(document);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        if (FailNextSave is { } failure)
        {
            FailNextSave = null;
            throw failure;
        }

        SaveCount++;
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryStorage : IDocumentStorage
{
    public Dictionary<string, byte[]> Blobs { get; } = [];

    public string Put(byte[] content)
    {
        var key = Guid.NewGuid().ToString("N");
        Blobs[key] = content;
        return key;
    }

    public async Task<string> SaveAsync(Stream content, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        return Put(buffer.ToArray());
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken) =>
        Task.FromResult<Stream>(new MemoryStream(Blobs[storageKey], writable: false));

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        Blobs.Remove(storageKey);
        return Task.CompletedTask;
    }
}

internal sealed class RecordingAuditLog : IAuditLog
{
    public List<(Guid DocumentId, Guid UserId, AuditAction Action, Guid? TargetUserId)> Entries { get; } = [];

    public void Record(Guid documentId, Guid userId, AuditAction action, Guid? targetUserId = null) =>
        Entries.Add((documentId, userId, action, targetUserId));
}

internal sealed class RecordingQueue : IProcessingQueue
{
    public List<Guid> Enqueued { get; } = [];

    public ValueTask EnqueueAsync(Guid documentId, CancellationToken cancellationToken)
    {
        Enqueued.Add(documentId);
        return ValueTask.CompletedTask;
    }
}

internal sealed class InMemoryUsers(params User[] users) : IUserRepository
{
    public Task<User?> FindByUserNameAsync(string userName, CancellationToken cancellationToken) =>
        Task.FromResult(Array.Find(users, u => u.UserName == userName));

    public Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(Array.Exists(users, u => u.Id == userId));

    public Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<User>>(users);
}

internal sealed class StubProcessor(string contentType, string signature) : IDocumentProcessor
{
    private readonly byte[] _signature = Encoding.ASCII.GetBytes(signature);

    public string ContentType => contentType;
    public ProcessingResult Result { get; set; } = new(PageCount: 1, Title: null, Author: null, TextPreview: "stub");
    public Exception? Failure { get; set; }
    public int Calls { get; private set; }

    public bool IsMatch(ReadOnlySpan<byte> header) => header.StartsWith(_signature);

    public Task<ProcessingResult> ProcessAsync(Stream content, CancellationToken cancellationToken)
    {
        Calls++;
        return Failure is null ? Task.FromResult(Result) : Task.FromException<ProcessingResult>(Failure);
    }
}
