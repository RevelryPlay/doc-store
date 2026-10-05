using DocStore.Core.Auditing;
using DocStore.Core.Common;
using DocStore.Core.Documents;
using DocStore.UnitTests.Fakes;

namespace DocStore.UnitTests.Documents;

public class DocumentReaderTests
{
    private readonly SharingScenario _scenario = new();
    private readonly DocumentReader _reader;

    public DocumentReaderTests() =>
        _reader = new DocumentReader(_scenario.Documents, _scenario.Storage, _scenario.Audit, _scenario.Documents);

    [Fact]
    public async Task GetByLink_ForSignedInUser_ReturnsDetailsWithoutSharingState()
    {
        var result = await _reader.GetByLinkAsync(_scenario.LinkToken, _scenario.User3.Id, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(_scenario.DocB.Id, result.Value.Id);
        Assert.Equal(AccessLevel.LinkHolder, result.Value.Access);
        Assert.Null(result.Value.ShareLinkToken);
        Assert.Empty(result.Value.SharedWith);
    }

    [Fact]
    public async Task GetByLink_WithUnknownToken_IsNotFound()
    {
        var result = await _reader.GetByLinkAsync("not-a-token", _scenario.User3.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ErrorKind.NotFound, result.Error?.Kind);
    }

    [Fact]
    public async Task Get_ForOwner_IncludesSharingState()
    {
        var result = await _reader.GetAsync(_scenario.DocC.Id, _scenario.User3.Id, linkToken: null, TestContext.Current.CancellationToken);

        Assert.Equal([_scenario.User1.Id, _scenario.User2.Id], result.Value.SharedWith);
    }

    [Fact]
    public async Task Open_ForStranger_IsNotFoundAndNotAudited()
    {
        var result = await _reader.OpenAsync(_scenario.DocA.Id, _scenario.User3.Id, linkToken: null, TestContext.Current.CancellationToken);

        Assert.Equal(ErrorKind.NotFound, result.Error?.Kind);
        Assert.Empty(_scenario.Audit.Entries);
    }

    [Fact]
    public async Task Open_ViaLink_ReturnsContentAndAuditsTheReader()
    {
        var result = await _reader.OpenAsync(_scenario.DocB.Id, _scenario.User1.Id, _scenario.LinkToken, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        await using var content = result.Value.Content;
        Assert.Equal(3, content.Length);
        Assert.Equal((_scenario.DocB.Id, _scenario.User1.Id, AuditAction.Downloaded, (Guid?)null), Assert.Single(_scenario.Audit.Entries));
    }
}
