using DayKeeper.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace DayKeeper.Infrastructure.Services;

/// <summary>
/// PVC-backed filesystem implementation of <see cref="IAttachmentStorageService"/>.
/// Files are stored under a configurable base path resolved from configuration key
/// <c>AttachmentStorage:BasePath</c>.
/// </summary>
public sealed class AttachmentStorageService : IAttachmentStorageService
{
    private readonly string _basePath;

    public AttachmentStorageService(IConfiguration configuration)
    {
        _basePath = Path.GetFullPath(
            configuration["AttachmentStorage:BasePath"]
            ?? throw new InvalidOperationException("AttachmentStorage:BasePath is not configured."));
    }

    /// <inheritdoc />
    public async Task SaveAsync(string storagePath, Stream content, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(storagePath);
        var directory = Path.GetDirectoryName(fullPath)!;

        Directory.CreateDirectory(directory);

        var fileStream = new FileStream(
            fullPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await using (fileStream.ConfigureAwait(false))
        {
            await content.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public Task<Stream> ReadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(storagePath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Attachment file not found.", fullPath);
        }

        Stream stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);

        return Task.FromResult(stream);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(storagePath);

        if (!File.Exists(fullPath))
        {
            return Task.FromResult(false);
        }

        File.Delete(fullPath);
        return Task.FromResult(true);
    }

    private string GetFullPath(string storagePath)
    {
        var normalized = Path.GetFullPath(Path.Combine(_basePath, storagePath));

        if (!normalized.StartsWith(_basePath, StringComparison.Ordinal))
        {
            throw new ArgumentException("Invalid storage path.", nameof(storagePath));
        }

        return normalized;
    }
}
