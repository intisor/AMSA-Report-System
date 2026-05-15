using AMSAReportingSystem.Data.Entities;

namespace AMSAReportingSystem.Services;

/// <summary>
/// Composite DTO: Combines unit-level Report data with state-level leadership context
/// Sent to National leadership for comprehensive report review
/// </summary>
public record ReportWithStateContext(
    int ReportId,
    int UnitId,
    int StateId,
    string CycleLabel,
    ReportStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? SubmittedToPresidentAt,
    DateTime? ApprovedByPresidentAt,
    string? PresidentialNotes,
    DateTime? ApprovedByStateAt,
    string? StateNotes,
    DateTime? AcknowledgedByNationalAt,
    string? NationalNotes,

    // State Leadership Context (NEW - from StateReport)
    StateReportContext? StateContext,

    // Department data (original)
    List<ReportDepartmentView> Departments,

    // Activity timeline
    List<ReportActivityView> ActivityLogs);

/// <summary>
/// State leadership form and aggregated metrics
/// Shown to National as context for the unit report
/// </summary>
public record StateReportContext(
    int StateReportId,
    string CycleLabel,
    int UnitsAttendedTo,
    int TotalUnitReportsCount,
    int? UnitPerformanceRating,
    decimal UnitPresidentAttendanceRate,

    // Leadership Form (State's Commentary)
    string? UnitImprovementPlan,
    string? ChallengesFaced,
    string? NationalSupportNeeded,
    string? OtherNotes,

    // State approval chain
    string? PresidentialNote,
    DateTime? ApprovedByPresidentAt,
    string? NationalNote,
    DateTime? AcknowledgedByNationalAt,

    // Aggregation metadata
    bool IsAggregated,
    DateTime? LastAggregatedAt,

    // Aggregated programs (state-level metrics)
    List<StateReportProgramView> Programs);

/// <summary>
/// Program row from state aggregation (for display)
/// </summary>
public record StateReportProgramView(
    int ProgramId,
    string ProgramName,
    string? Objectives,
    string? Outcomes,
    int? TotalAttendance,
    int? TotalBeneficiaries,
    bool IsAutoAggregated);
