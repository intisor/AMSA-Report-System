using AMSAReportingSystem.Data.Entities;

namespace AMSAReportingSystem.Services;

public sealed class StateReportForm
{
    public int StateReportId { get; set; }
    public int UnitPresidentsAttended { get; set; }
    public int TotalUnitPresidents { get; set; }
    public int? UnitPerformanceRating { get; set; }
    public string? UnitImprovementPlan { get; set; }
    public string? ChallengesFaced { get; set; }
    public string? NationalSupportNeeded { get; set; }
    public string? OtherNotes { get; set; }
    public List<StateProgramForm> Programs { get; set; } = new();
}

public sealed class StateProgramForm
{
    public string ProgramName { get; set; } = string.Empty;
    public string? Objectives { get; set; }
    public string? Outcomes { get; set; }
    public int? TotalAttendance { get; set; }
    public int? TotalBeneficiaries { get; set; }
}

public static class StateReportFormMapper
{
    public static StateReportForm ToForm(StateReport report)
    {
        return new StateReportForm
        {
            StateReportId = report.Id,
            UnitPresidentsAttended = report.UnitPresidentsAttended,
            TotalUnitPresidents = report.TotalUnitPresidents,
            UnitPerformanceRating = report.UnitPerformanceRating,
            UnitImprovementPlan = report.UnitImprovementPlan,
            ChallengesFaced = report.ChallengesFaced,
            NationalSupportNeeded = report.NationalSupportNeeded,
            OtherNotes = report.OtherNotes,
            Programs = report.Programs
                .OrderBy(p => p.ProgramId)
                .Select(p => new StateProgramForm
                {
                    ProgramName = p.ProgramName,
                    Objectives = p.Objectives,
                    Outcomes = p.Outcomes,
                    TotalAttendance = p.TotalAttendance,
                    TotalBeneficiaries = p.TotalBeneficiaries
                })
                .ToList()
        };
    }
}
