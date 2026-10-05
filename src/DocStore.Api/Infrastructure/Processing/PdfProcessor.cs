using System.Text;
using DocStore.Core.Documents;
using DocStore.Core.Processing;
using UglyToad.PdfPig;

namespace DocStore.Api.Infrastructure.Processing;

public sealed class PdfProcessor : IDocumentProcessor
{
    public string ContentType => "application/pdf";

    public bool IsMatch(ReadOnlySpan<byte> header) => header.StartsWith("%PDF-"u8);

    public Task<ProcessingResult> ProcessAsync(Stream content, CancellationToken cancellationToken)
    {
        using var pdf = PdfDocument.Open(content);
        var preview = new StringBuilder();

        // Only read as many pages as the preview needs; large PDFs should not be fully parsed.
        foreach (var page in pdf.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            AppendWords(preview, page.GetWords().Select(w => w.Text));
            if (preview.Length >= Document.MaxPreviewLength)
            {
                break;
            }
        }

        return Task.FromResult(new ProcessingResult(
            pdf.NumberOfPages,
            NullIfBlank(pdf.Information.Title),
            NullIfBlank(pdf.Information.Author),
            NullIfBlank(preview.ToString())));
    }

    private static void AppendWords(StringBuilder preview, IEnumerable<string> words)
    {
        foreach (var word in words)
        {
            if (preview.Length > 0)
            {
                preview.Append(' ');
            }

            preview.Append(word);
        }
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
