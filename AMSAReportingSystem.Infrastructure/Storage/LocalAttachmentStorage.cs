using AMSAReportingSystem.Core.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AMSAReportingSystem.Infrastructure.Storage;

public sealed class LocalAttachmentStorage(IHostEnvironment hostEnvironment, IOptions<LocalAttachmentStorageOptions> options) : IAttachmentStorage
{
    private readonly string _rootPath = Path.IsPathRooted(options.Value.RootPath)
        ? options.Value.RootPath
        : Path.Combine(hostEnvironment.ContentRootPath, options.Value.RootPath);

    public async Task<AttachmentStorageItem> SaveAsync(string scope, string ownerId, string fileName, string contentType, byte[] content, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(content);

        var extension = Path.GetExtension(fileName);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var relativePath = Path.Combine(scope, ownerId, storedFileName);
        var fullPath = Path.Combine(_rootPath, relativePath);
        var directory = Path.GetDirectoryName(fullPath) ?? _rootPath;

        Directory.CreateDirectory(directory);
        await File.WriteAllBytesAsync(fullPath, content, ct);

        return new AttachmentStorageItem(relativePath, string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType, content.LongLength);
    }

    public Task<AttachmentReadStream> OpenReadAsync(string relativePath, string? contentType = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        ct.ThrowIfCancellationRequested();

        var fullPath = Path.Combine(_rootPath, relativePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Attachment file was not found.", relativePath);
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        return Task.FromResult(new AttachmentReadStream(stream, contentType ?? "application/octet-stream"));
    }

    public Task DeleteAsync(string relativePath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        var fullPath = Path.Combine(_rootPath, relativePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}