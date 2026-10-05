namespace DocStore.Core.Processing;

public sealed class DocumentProcessors(IEnumerable<IDocumentProcessor> processors)
{
    public const int HeaderLength = 16;

    private readonly IDocumentProcessor[] _processors = [.. processors];

    public IDocumentProcessor? Detect(ReadOnlySpan<byte> header)
    {
        foreach (var processor in _processors)
        {
            if (processor.IsMatch(header))
            {
                return processor;
            }
        }

        return null;
    }

    public IDocumentProcessor? For(string contentType) =>
        Array.Find(_processors, p => p.ContentType == contentType);
}
