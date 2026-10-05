namespace DocStore.Core.Processing;

/// <summary>
/// Supporting a new file format means adding one implementation and registering it.
/// Upload detection and background processing both discover formats through these.
/// </summary>
public interface IDocumentProcessor
{
    string ContentType { get; }

    /// <param name="header">Up to <see cref="DocumentProcessors.HeaderLength"/> leading bytes of the file.</param>
    bool IsMatch(ReadOnlySpan<byte> header);

    Task<ProcessingResult> ProcessAsync(Stream content, CancellationToken cancellationToken);
}

public sealed record ProcessingResult(int? PageCount, string? Title, string? Author, string? TextPreview);
