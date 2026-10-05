using System.Text;
using DocStore.Core.Auditing;
using DocStore.Core.Common;
using DocStore.Core.Documents;
using DocStore.Core.Processing;
using DocStore.UnitTests.Fakes;

namespace DocStore.UnitTests.Documents;

public class UploadDocumentTests
{
    private static readonly Guid Owner = Guid.NewGuid();

    private readonly InMemoryDocuments _documents = new();
    private readonly InMemoryStorage _storage = new();
    private readonly RecordingQueue _queue = new();
    private readonly RecordingAuditLog _audit = new();
    private readonly UploadDocument _upload;

    public UploadDocumentTests()
    {
        var processors = new DocumentProcessors([new StubProcessor("application/pdf", "%PDF-")]);
        _upload = new UploadDocument(
            _documents, _storage, processors, _queue, _audit, _documents, TimeProvider.System, new UploadOptions { MaxSizeBytes = 64 });
    }

    [Fact]
    public async Task Upload_RecognizedFile_StoresQueuesAndAudits()
    {
        var result = await _upload.ExecuteAsync(Owner, "report.pdf", Content("%PDF-1.7 body"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        var document = Assert.Single(_documents.Items);
        Assert.Equal(result.Value, document.Id);
        Assert.Equal("application/pdf", document.ContentType);
        Assert.Equal(ProcessingStatus.Pending, document.Status);
        Assert.Equal("%PDF-1.7 body", Encoding.ASCII.GetString(_storage.Blobs[document.StorageKey]));
        Assert.Equal([document.Id], _queue.Enqueued);
        Assert.Equal((document.Id, Owner, AuditAction.Uploaded, (Guid?)null), Assert.Single(_audit.Entries));
    }

    [Theory]
    [InlineData("")]
    [InlineData("MZ not a pdf")]
    public async Task Upload_EmptyOrUnrecognized_IsRejectedWithoutSideEffects(string body)
    {
        var result = await _upload.ExecuteAsync(Owner, "renamed.pdf", Content(body), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorKind.Invalid, result.Error?.Kind);
        Assert.Empty(_documents.Items);
        Assert.Empty(_storage.Blobs);
        Assert.Empty(_queue.Enqueued);
    }

    [Fact]
    public async Task Upload_OverSizeLimit_IsRejected()
    {
        var result = await _upload.ExecuteAsync(Owner, "big.pdf", Content("%PDF-" + new string('x', 64)), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorKind.Invalid, result.Error?.Kind);
        Assert.Empty(_storage.Blobs);
    }

    [Fact]
    public async Task Upload_WhenSaveFails_RemovesStoredContent()
    {
        _documents.FailNextSave = new InvalidOperationException("db down");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _upload.ExecuteAsync(Owner, "report.pdf", Content("%PDF-1.7"), TestContext.Current.CancellationToken));

        Assert.Empty(_storage.Blobs);
        Assert.Empty(_queue.Enqueued);
    }

    private static MemoryStream Content(string body) => new(Encoding.ASCII.GetBytes(body));
}
