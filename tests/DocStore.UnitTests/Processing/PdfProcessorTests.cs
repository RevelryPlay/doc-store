using DocStore.Api.Infrastructure.Processing;
using DocStore.Api.Seeding;
using DocStore.Core.Documents;

namespace DocStore.UnitTests.Processing;

public class PdfProcessorTests
{
    private readonly PdfProcessor _processor = new();

    [Fact]
    public async Task Process_ExtractsPageCountMetadataAndPreview()
    {
        using var pdf = new MemoryStream(SamplePdf.Create("Quarterly review", "Avery Chen", "First line", "Second line"));

        var result = await _processor.ProcessAsync(pdf, TestContext.Current.CancellationToken);

        Assert.Equal(1, result.PageCount);
        Assert.Equal("Quarterly review", result.Title);
        Assert.Equal("Avery Chen", result.Author);
        Assert.Contains("Second line", result.TextPreview, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Process_LongDocument_FlowsOntoMultiplePagesAndCapsPreview()
    {
        var paragraphs = Enumerable.Repeat(string.Join(' ', Enumerable.Repeat("lorem ipsum dolor", 40)), 30).ToArray();
        using var pdf = new MemoryStream(SamplePdf.Create("Long", "Author", paragraphs));

        var result = await _processor.ProcessAsync(pdf, TestContext.Current.CancellationToken);

        Assert.True(result.PageCount > 1);
        Assert.True(result.TextPreview!.Length >= Document.MaxPreviewLength);
    }

    [Fact]
    public void IsMatch_RequiresPdfSignature()
    {
        Assert.True(_processor.IsMatch("%PDF-1.7\n"u8));
        Assert.False(_processor.IsMatch("PK\u0003\u0004"u8));
        Assert.False(_processor.IsMatch("%PD"u8));
    }

    [Fact]
    public async Task Process_CorruptPdf_Throws()
    {
        using var notReallyPdf = new MemoryStream("%PDF-1.7 truncated"u8.ToArray());

        await Assert.ThrowsAnyAsync<Exception>(() => _processor.ProcessAsync(notReallyPdf, TestContext.Current.CancellationToken));
    }
}
