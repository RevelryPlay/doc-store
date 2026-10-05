using DocStore.Core.Auditing;
using DocStore.Core.Common;
using DocStore.Core.Documents;
using DocStore.UnitTests.Fakes;

namespace DocStore.UnitTests.Documents;

public class DocumentReaderTests
{
    private readonly SharingScenario _scenario = new();
    private readonly StubQueries _queries = new();
    private readonly DocumentReader _reader;

    public DocumentReaderTests() =>
        _reader = new DocumentReader(_scenario.Documents, _queries, _scenario.Storage, _scenario.Audit, _scenario.Documents);

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
    public async Task GetByLink_RecordsTheViewer()
    {
        await _reader.GetByLinkAsync(_scenario.LinkToken, _scenario.User3.Id, TestContext.Current.CancellationToken);

        Assert.Equal((_scenario.DocB.Id, _scenario.User3.Id, AuditAction.Viewed, (Guid?)null), Assert.Single(_scenario.Audit.Entries));
    }

    [Fact]
    public async Task Get_BySharedUser_RecordsView()
    {
        await _reader.GetAsync(_scenario.DocC.Id, _scenario.User1.Id, linkToken: null, TestContext.Current.CancellationToken);

        Assert.Equal(AuditAction.Viewed, Assert.Single(_scenario.Audit.Entries).Action);
        Assert.Equal(1, _scenario.Documents.SaveCount);
    }

    [Fact]
    public async Task Get_ByOwner_IsNotRecorded()
    {
        await _reader.GetAsync(_scenario.DocC.Id, _scenario.User3.Id, linkToken: null, TestContext.Current.CancellationToken);

        Assert.Empty(_scenario.Audit.Entries);
        Assert.Equal(0, _scenario.Documents.SaveCount);
    }

    [Fact]
    public async Task Get_ByStranger_IsNotFoundAndNotRecorded()
    {
        var result = await _reader.GetAsync(_scenario.DocA.Id, _scenario.User3.Id, linkToken: null, TestContext.Current.CancellationToken);

        Assert.Equal(ErrorKind.NotFound, result.Error?.Kind);
        Assert.Empty(_scenario.Audit.Entries);
    }

    [Fact]
    public async Task GetActivity_ForOwner_ReturnsEntries()
    {
        _queries.Activity.Add(new DocumentActivity(AuditAction.Viewed, "User One", null, DateTimeOffset.UnixEpoch));

        var result = await _reader.GetActivityAsync(_scenario.DocC.Id, _scenario.User3.Id, TestContext.Current.CancellationToken);

        Assert.Equal("User One", Assert.Single(result.Value).ActorName);
    }

    [Theory]
    [InlineData(2, ErrorKind.Forbidden)]
    [InlineData(3, ErrorKind.NotFound)]
    public async Task GetActivity_ForNonOwner_IsDenied(int user, ErrorKind expected)
    {
        var result = await _reader.GetActivityAsync(_scenario.DocA.Id, _scenario.User(user).Id, TestContext.Current.CancellationToken);

        Assert.Equal(expected, result.Error?.Kind);
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
