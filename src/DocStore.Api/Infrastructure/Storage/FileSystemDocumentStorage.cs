using DocStore.Core.Storage;

namespace DocStore.Api.Infrastructure.Storage;

public sealed class StorageOptions
{
    public string RootPath { get; init; } = "App_Data/documents";
}

/// <summary>Content is stored under random keys; user-supplied file names never reach the file system.</summary>
public sealed class FileSystemDocumentStorage : IDocumentStorage
{
    private readonly string _root;

    public FileSystemDocumentStorage(StorageOptions options, IHostEnvironment environment)
    {
        _root = Path.GetFullPath(options.RootPath, environment.ContentRootPath);
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Stream content, CancellationToken cancellationToken)
    {
        var key = Guid.NewGuid().ToString("N");
        await using var file = new FileStream(PathFor(key), FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await content.CopyToAsync(file, cancellationToken);
        return key;
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken) =>
        Task.FromResult<Stream>(new FileStream(PathFor(storageKey), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true));

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        File.Delete(PathFor(storageKey));
        return Task.CompletedTask;
    }

    private string PathFor(string storageKey) => Path.Combine(_root, storageKey);
}
