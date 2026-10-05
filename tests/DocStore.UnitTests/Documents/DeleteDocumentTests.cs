using DocStore.Core.Auditing;
using DocStore.Core.Common;
using DocStore.Core.Documents;
using DocStore.UnitTests.Fakes;

namespace DocStore.UnitTests.Documents;

public class DeleteDocumentTests
{
    private readonly SharingScenario _scenario = new();
    private readonly DeleteDocument _delete;

    public DeleteDocumentTests() =>
        _delete = new DeleteDocument(_scenario.Documents, _scenario.Storage, _scenario.Audit, _scenario.Documents);

    [Fact]
    public async Task Delete_ByOwner_RemovesRowAndContent()
    {
        var storageKey = _scenario.DocC.StorageKey;

        var result = await _delete.ExecuteAsync(_scenario.DocC.Id, _scenario.User3.Id, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(_scenario.DocC, _scenario.Documents.Items);
        Assert.False(_scenario.Storage.Blobs.ContainsKey(storageKey));
        Assert.Equal(AuditAction.Deleted, Assert.Single(_scenario.Audit.Entries).Action);
    }

    [Fact]
    public async Task Delete_BySharedUser_IsForbiddenAndKeepsDocument()
    {
        var result = await _delete.ExecuteAsync(_scenario.DocC.Id, _scenario.User1.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ErrorKind.Forbidden, result.Error?.Kind);
        Assert.Contains(_scenario.DocC, _scenario.Documents.Items);
        Assert.True(_scenario.Storage.Blobs.ContainsKey(_scenario.DocC.StorageKey));
    }
}
