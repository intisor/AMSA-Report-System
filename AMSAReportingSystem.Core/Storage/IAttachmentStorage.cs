namespace AMSAReportingSystem.Core.Storage;

public interface IAttachmentStorage
{
    Task<AttachmentStorageItem> SaveAsync(string scope, string ownerId, string fileName, string contentType, byte[] content, CancellationToken ct = default);
    Task<AttachmentReadStream> OpenReadAsync(string relativePath, string? contentType = null, CancellationToken ct = default);
    Task DeleteAsync(string relativePath, CancellationToken ct = default);
}