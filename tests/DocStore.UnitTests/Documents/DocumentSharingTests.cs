using DocStore.Core.Auditing;
using DocStore.Core.Common;
using DocStore.Core.Documents;
using DocStore.UnitTests.Fakes;

namespace DocStore.UnitTests.Documents;

public class DocumentSharingTests
{
    private readonly SharingScenario _scenario = new();
    private readonly DocumentSharing _sharing;

    public DocumentSharingTests() =>
        _sharing = new DocumentSharing(_scenario.Documents, _scenario.Users, _scenario.Audit, _scenario.Documents);

    [Fact]
    public async Task Share_ByOwner_GrantsAccessAndAudits()
    {
        var result = await _sharing.ShareAsync(_scenario.DocA.Id, _scenario.User1.Id, _scenario.User3.Id, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(_scenario.DocA.IsSharedWith(_scenario.User3.Id));
        var entry = Assert.Single(_scenario.Audit.Entries);
        Assert.Equal((AuditAction.Shared, _scenario.User3.Id), (entry.Action, entry.TargetUserId));
        Assert.Equal(1, _scenario.Documents.SaveCount);
    }

    [Fact]
    public async Task Share_BySharedUser_IsForbiddenAndUnchanged()
    {
        var result = await _sharing.ShareAsync(_scenario.DocA.Id, _scenario.User2.Id, _scenario.User3.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ErrorKind.Forbidden, result.Error?.Kind);
        Assert.False(_scenario.DocA.IsSharedWith(_scenario.User3.Id));
        Assert.Equal(0, _scenario.Documents.SaveCount);
    }

    [Fact]
    public async Task Share_WithUnknownUser_IsInvalid()
    {
        var result = await _sharing.ShareAsync(_scenario.DocA.Id, _scenario.User1.Id, Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorKind.Invalid, result.Error?.Kind);
    }

    [Fact]
    public async Task Share_ByStrangerWithUnknownUser_IsNotFound()
    {
        var result = await _sharing.ShareAsync(_scenario.DocA.Id, _scenario.User3.Id, Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(ErrorKind.NotFound, result.Error?.Kind);
    }

    [Fact]
    public async Task Share_WithOwner_IsInvalidAndNotSaved()
    {
        var result = await _sharing.ShareAsync(_scenario.DocA.Id, _scenario.User1.Id, _scenario.User1.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ErrorKind.Invalid, result.Error?.Kind);
        Assert.Equal(0, _scenario.Documents.SaveCount);
    }

    [Fact]
    public async Task Unshare_ByOwner_RevokesAccess()
    {
        var result = await _sharing.UnshareAsync(_scenario.DocC.Id, _scenario.User3.Id, _scenario.User1.Id, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.False(_scenario.DocC.IsSharedWith(_scenario.User1.Id));
        Assert.True(_scenario.DocC.IsSharedWith(_scenario.User2.Id));
    }

    [Fact]
    public async Task EnableLink_ByOwner_ReturnsToken()
    {
        var result = await _sharing.EnableLinkAsync(_scenario.DocA.Id, _scenario.User1.Id, TestContext.Current.CancellationToken);

        Assert.Equal(_scenario.DocA.ShareLinkToken, result.Value);
    }

    [Fact]
    public async Task DisableLink_ByStranger_IsNotFound()
    {
        var result = await _sharing.DisableLinkAsync(_scenario.DocB.Id, _scenario.User3.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ErrorKind.NotFound, result.Error?.Kind);
        Assert.NotNull(_scenario.DocB.ShareLinkToken);
    }
}
