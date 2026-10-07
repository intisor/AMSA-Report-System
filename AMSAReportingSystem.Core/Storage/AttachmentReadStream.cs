namespace AMSAReportingSystem.Core.Storage;

public sealed class AttachmentReadStream(Stream content, string contentType)
{
    public Stream Content { get; } = content;
    public string ContentType { get; } = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType;
}
