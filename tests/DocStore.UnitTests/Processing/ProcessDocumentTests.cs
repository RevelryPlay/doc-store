using DocStore.Core.Documents;
using DocStore.Core.Processing;
using DocStore.UnitTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocStore.UnitTests.Processing;

public class ProcessDocumentTests
{
    private readonly InMemoryDocuments _documents = new();
    private readonly InMemoryStorage _storage = new();
    private readonly StubProcessor _pdf = new("application/pdf", "%PDF-");
    private readonly StubProcessor _other = new("application/x-other", "OTHER");

    [Fact]
    public async Task Execute_UsesProcessorMatchingContentType()
    {
        var document = AddDocument("application/x-other");

        await NewProcessDocument().ExecuteAsync(document.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ProcessingStatus.Ready, document.Status);
        Assert.Equal(1, _other.Calls);
        Assert.Equal(0, _pdf.Calls);
    }

    [Fact]
    public async Task Execute_WhenProcessorThrows_MarksFailed()
    {
        _pdf.Failure = new InvalidDataException("corrupt");
        var document = AddDocument("application/pdf");

        await NewProcessDocument().ExecuteAsync(document.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ProcessingStatus.Failed, document.Status);
        Assert.Equal(1, _documents.SaveCount);
    }

    [Fact]
    public async Task Execute_WithNoMatchingProcessor_MarksFailed()
    {
        var document = AddDocument("application/x-unknown");

        await NewProcessDocument().ExecuteAsync(document.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ProcessingStatus.Failed, document.Status);
    }

    [Fact]
    public async Task Execute_ForAlreadyProcessedDocument_DoesNothing()
    {
        var document = AddDocument("application/pdf");
        document.MarkFailed();

        await NewProcessDocument().ExecuteAsync(document.Id, TestContext.Current.CancellationToken);

        Assert.Equal(0, _pdf.Calls);
        Assert.Equal(0, _documents.SaveCount);
    }

    [Fact]
    public void Detect_PicksProcessorBySignature()
    {
        var processors = new DocumentProcessors([_pdf, _other]);

        Assert.Same(_other, processors.Detect("OTHER..."u8));
        Assert.Same(_pdf, processors.Detect("%PDF-1.7"u8));
        Assert.Null(processors.Detect("nope"u8));
    }

    private ProcessDocument NewProcessDocument() =>
        new(_documents, _storage, new DocumentProcessors([_pdf, _other]), _documents, NullLogger<ProcessDocument>.Instance);

    private Document AddDocument(string contentType)
    {
        var document = new Document(Guid.NewGuid(), "file", contentType, 3, _storage.Put([1, 2, 3]), DateTimeOffset.UnixEpoch);
        _documents.Add(document);
        return document;
    }
}
