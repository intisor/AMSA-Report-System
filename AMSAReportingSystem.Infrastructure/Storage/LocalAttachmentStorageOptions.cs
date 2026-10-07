namespace AMSAReportingSystem.Infrastructure.Storage;

public sealed class LocalAttachmentStorageOptions
{
    public string RootPath { get; set; } = Path.Combine("App_Data", "attachments");
}