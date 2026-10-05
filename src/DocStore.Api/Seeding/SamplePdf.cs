using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace DocStore.Api.Seeding;

/// <summary>Builds real, text-bearing PDFs for seeding and tests. Paragraphs wrap and flow onto new pages.</summary>
public static class SamplePdf
{
    private const double Margin = 72;
    private const double TitleSize = 18;
    private const double BodySize = 11;
    private const double LineHeight = 16;
    private const int MaxLineChars = 90;

    public static byte[] Create(string title, string author, params string[] paragraphs)
    {
        var builder = new PdfDocumentBuilder();
        builder.DocumentInformation.Title = title;
        builder.DocumentInformation.Author = author;

        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        var page = builder.AddPage(PageSize.Letter);
        var top = page.PageSize.Height - Margin;
        page.AddText(title, TitleSize, new PdfPoint(Margin, top), font);
        var y = top - (LineHeight * 2);

        foreach (var paragraph in paragraphs)
        {
            foreach (var line in Wrap(paragraph))
            {
                if (y < Margin)
                {
                    page = builder.AddPage(PageSize.Letter);
                    y = top;
                }

                page.AddText(line, BodySize, new PdfPoint(Margin, y), font);
                y -= LineHeight;
            }

            y -= LineHeight / 2;
        }

        return builder.Build();
    }

    private static IEnumerable<string> Wrap(string paragraph)
    {
        var line = "";
        foreach (var word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Length > 0 && line.Length + 1 + word.Length > MaxLineChars)
            {
                yield return line;
                line = word;
            }
            else
            {
                line = line.Length == 0 ? word : $"{line} {word}";
            }
        }

        if (line.Length > 0)
        {
            yield return line;
        }
    }
}
