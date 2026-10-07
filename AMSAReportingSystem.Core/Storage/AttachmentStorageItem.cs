namespace AMSAReportingSystem.Core.Storage;

public sealed record AttachmentStorageItem(
    string RelativePath,
    string ContentType,
    long FileSizeBytes);