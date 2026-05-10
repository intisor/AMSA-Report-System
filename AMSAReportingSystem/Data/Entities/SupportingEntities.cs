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
