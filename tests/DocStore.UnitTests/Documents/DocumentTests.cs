using DocStore.Core.Common;
using DocStore.Core.Documents;
using DocStore.Core.Processing;

namespace DocStore.UnitTests.Documents;

public class DocumentTests
{
    private static readonly Guid Owner = Guid.NewGuid();
    private static readonly Guid Other = Guid.NewGuid();

    [Fact]
    public void ShareWith_Owner_IsRejected()
    {
        var result = NewDocument().ShareWith(Owner);

        Assert.Equal(ErrorKind.Invalid, result.Error?.Kind);
    }

    [Fact]
    public void ShareWith_SameUserTwice_KeepsOneShare()
    {
        var document = NewDocument();

        document.ShareWith(Other);
        document.ShareWith(Other);

        Assert.Single(document.Shares);
    }

    [Fact]
    public void Unshare_RemovesShare()
    {
        var document = NewDocument();
        document.ShareWith(Other);

        document.Unshare(Other);

        Assert.False(document.IsSharedWith(Other));
    }

    [Fact]
    public void EnableLinkSharing_IsStableUntilDisabled()
    {
        var document = NewDocument();

        var first = document.EnableLinkSharing();
        var second = document.EnableLinkSharing();
        document.DisableLinkSharing();
        var third = document.EnableLinkSharing();

        Assert.Equal(first, second);
        Assert.NotEqual(first, third);
        Assert.Equal(43, first.Length);
    }

    [Theory]
    [InlineData("report.pdf", "report.pdf")]
    [InlineData("../../etc/passwd", "passwd")]
    [InlineData(@"C:\Users\me\report.pdf", "report.pdf")]
    [InlineData("   ", "document")]
    public void FileName_IsReducedToDisplayName(string input, string expected)
    {
        Assert.Equal(expected, NewDocument(input).FileName);
    }

    [Fact]
    public void MarkProcessed_TruncatesPreviewAndSetsReady()
    {
        var document = NewDocument();

        var result = document.MarkProcessed(new ProcessingResult(3, "Title", "Author", new string('x', Document.MaxPreviewLength + 50)));

        Assert.True(result.IsSuccess);
        Assert.Equal(ProcessingStatus.Ready, document.Status);
        Assert.Equal(3, document.PageCount);
        Assert.Equal(Document.MaxPreviewLength, document.TextPreview?.Length);
    }

    [Fact]
    public void MarkProcessed_WhenAlreadyFailed_IsRejected()
    {
        var document = NewDocument();
        document.MarkFailed();

        var result = document.MarkProcessed(new ProcessingResult(1, null, null, null));

        Assert.False(result.IsSuccess);
        Assert.Equal(ProcessingStatus.Failed, document.Status);
    }

    private static Document NewDocument(string fileName = "file.pdf") =>
        new(Owner, fileName, "application/pdf", 10, "key", DateTimeOffset.UnixEpoch);
}
