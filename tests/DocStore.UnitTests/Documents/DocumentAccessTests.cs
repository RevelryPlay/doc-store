using DocStore.Core.Common;
using DocStore.Core.Documents;
using DocStore.UnitTests.Fakes;

namespace DocStore.UnitTests.Documents;

public class DocumentAccessTests
{
    private readonly SharingScenario _scenario = new();

    [Theory]
    [InlineData("A", 1, AccessLevel.Owner)]
    [InlineData("A", 2, AccessLevel.SharedUser)]
    [InlineData("A", 3, AccessLevel.None)]
    [InlineData("B", 1, AccessLevel.None)]
    [InlineData("B", 2, AccessLevel.Owner)]
    [InlineData("B", 3, AccessLevel.None)]
    [InlineData("C", 1, AccessLevel.SharedUser)]
    [InlineData("C", 2, AccessLevel.SharedUser)]
    [InlineData("C", 3, AccessLevel.Owner)]
    public void Evaluate_WithoutLink_MatchesScenario(string document, int user, AccessLevel expected)
    {
        var access = DocumentAccess.Evaluate(_scenario[document], _scenario.User(user).Id, linkToken: null);

        Assert.Equal(expected, access);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void Evaluate_WithValidLink_GrantsLinkHolder(int user)
    {
        var access = DocumentAccess.Evaluate(_scenario.DocB, _scenario.User(user).Id, _scenario.LinkToken);

        Assert.Equal(AccessLevel.LinkHolder, access);
    }

    [Fact]
    public void Evaluate_WithAnotherDocumentsLink_GrantsNothing()
    {
        var access = DocumentAccess.Evaluate(_scenario.DocA, _scenario.User3.Id, _scenario.LinkToken);

        Assert.Equal(AccessLevel.None, access);
    }

    [Fact]
    public void Evaluate_AfterLinkDisabled_RevokesLinkHolder()
    {
        _scenario.DocB.DisableLinkSharing();

        var access = DocumentAccess.Evaluate(_scenario.DocB, _scenario.User3.Id, _scenario.LinkToken);

        Assert.Equal(AccessLevel.None, access);
    }

    [Fact]
    public void RequireOwner_ForSharedUser_IsForbidden()
    {
        var result = DocumentAccess.RequireOwner(_scenario.DocA, _scenario.User2.Id);

        Assert.Equal(ErrorKind.Forbidden, result.Error?.Kind);
    }

    [Fact]
    public void RequireOwner_ForStranger_IsNotFound()
    {
        var result = DocumentAccess.RequireOwner(_scenario.DocA, _scenario.User3.Id);

        Assert.Equal(ErrorKind.NotFound, result.Error?.Kind);
    }

    [Fact]
    public void RequireAccess_ForMissingDocument_IsNotFound()
    {
        var result = DocumentAccess.RequireAccess(null, _scenario.User1.Id, linkToken: null);

        Assert.Equal(ErrorKind.NotFound, result.Error?.Kind);
    }
}
