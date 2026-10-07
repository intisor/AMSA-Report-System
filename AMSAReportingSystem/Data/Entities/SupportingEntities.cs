namespace AMSAReportingSystem.Data.Entities;

/// <summary>
/// Audit trail for report workflow actions.
/// </summary>
public class ReportActivityLog
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public int ActionByMemberId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime ActionAt { get; set; } = DateTime.UtcNow;

    public Report Report { get; set; } = null!;
}

public class ReportAttachment
{
    public int Id { get; set; }
    public int DepartmentReportId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public int UploadedByMemberId { get; set; }

    public DepartmentReport DepartmentReport { get; set; } = null!;
}

public class StateReportAttachment
{
    public int Id { get; set; }
    public int StateReportId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public int UploadedByMemberId { get; set; }

    public StateReport StateReport { get; set; } = null!;
}
