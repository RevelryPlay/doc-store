namespace DocStore.Core.Storage;

public interface IDocumentStorage
{
    /// <returns>The storage key for the saved content.</returns>
    Task<string> SaveAsync(Stream content, CancellationToken cancellationToken);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
}
