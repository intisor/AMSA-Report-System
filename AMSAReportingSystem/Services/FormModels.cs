using AMSAReportingSystem.Data.Entities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AMSAReportingSystem.Services;

#region Department Report Forms

/// <summary>
/// Department report form models for structured data submission
/// Each department submits data through its corresponding form
/// </summary>

public sealed class TaleemForm
{
    public int? AttendanceCount { get; set; }
    public int? TotalMemberCount { get; set; }
    public string? BelowSeventyFiveReasons { get; set; }
    public int? SessionDurationMinutes { get; set; }
    public int? MonthlyTestParticipants { get; set; }
    public int? SessionsOrganized { get; set; }
    public string? Notes { get; set; }
}

public sealed class TablighForm
{
    public string? ActivitiesDetails { get; set; }
    public string? Purpose { get; set; }
    public int? AttendeeCount { get; set; }
    public string? Beneficiaries { get; set; }
    public bool HasOnCampusActivity { get; set; }
}

public sealed class WelfareForm
{
    public int? ProgramCount { get; set; }
    public int? ParticipantCount { get; set; }
    public string? ActivitiesWithDates { get; set; }
    public string? BriefReportWithBeneficiaries { get; set; }
}

public sealed class SportForm
{
    public string? GamesParticipated { get; set; }
    public int? MemberParticipantCount { get; set; }
    public int? NonMemberParticipantCount { get; set; }
    public int? TotalMemberCount { get; set; }
}

public sealed class FinanceForm
{
    public decimal? DuesCollected { get; set; }
    public string? NonPaymentReasons { get; set; }
    public decimal? ExpectedDues { get; set; }
}

public sealed class HealthForm
{
    public string? HealthActivityWithDate { get; set; }
    public int? BeneficiaryCount { get; set; }
}

public sealed class SecondarySchoolForm
{
    public bool AttendedLastJamaatAndCircuitMeeting { get; set; }
    public string? ParentSupportEfforts { get; set; }
    public string? StudentActivities { get; set; }
}

public sealed class TajneedForm
{
    public bool HasAccurateUpdatedTajneed { get; set; }
    public string? ImprovementEfforts { get; set; }
    public string? AccuracyPlans { get; set; }
}

public sealed class GeneralForm
{
    public string? ChallengesAndResolutionPlan { get; set; }
    public string? OtherActivitiesWithDates { get; set; }
}

#endregion

#region State Report Forms

/// <summary>
/// State-level report forms for aggregated unit performance data
/// Contains unit-wide metrics and state-specific programs
/// </summary>

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

#endregion

#region Form Serialization & Deserialization

/// <summary>
/// Handles serialization/deserialization of department report forms
/// Manages JSON conversion with camelCase policy and null-value handling
/// </summary>
public static class DepartmentReportFormMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Deserializes JSON to the appropriate department form type
    /// Returns an empty instance if deserialization fails
    /// </summary>
    public static object Deserialize(DepartmentType department, string? json)
    {
        var payload = string.IsNullOrWhiteSpace(json) ? "{}" : json!;
        return department switch
        {
            DepartmentType.Taleem => DeserializeOrDefault<TaleemForm>(payload),
            DepartmentType.Tabligh => DeserializeOrDefault<TablighForm>(payload),
            DepartmentType.Welfare => DeserializeOrDefault<WelfareForm>(payload),
            DepartmentType.Sport => DeserializeOrDefault<SportForm>(payload),
            DepartmentType.Finance => DeserializeOrDefault<FinanceForm>(payload),
            DepartmentType.Health => DeserializeOrDefault<HealthForm>(payload),
            DepartmentType.SecondarySchool => DeserializeOrDefault<SecondarySchoolForm>(payload),
            DepartmentType.Tajneed => DeserializeOrDefault<TajneedForm>(payload),
            DepartmentType.General => DeserializeOrDefault<GeneralForm>(payload),
            _ => new GeneralForm()
        };
    }

    /// <summary>
    /// Serializes department form to JSON
    /// Normalizes derived fields before serialization
    /// </summary>
    public static string Serialize(DepartmentType department, object form)
    {
        NormalizeDerivedFields(department, form);
        return JsonSerializer.Serialize(form, form.GetType(), JsonOptions);
    }

    /// <summary>
    /// Deserializes JSON string to typed form, returning empty instance on failure
    /// </summary>
    private static T DeserializeOrDefault<T>(string payload) where T : new()
    {
        try
        {
            return JsonSerializer.Deserialize<T>(payload, JsonOptions) ?? new T();
        }
        catch
        {
            return new T();
        }
    }

    /// <summary>
    /// Normalizes derived fields before serialization
    /// Example: Tabligh form checks for campus keywords in activities
    /// </summary>
    private static void NormalizeDerivedFields(DepartmentType department, object form)
    {
        if (department != DepartmentType.Tabligh || form is not TablighForm tab)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(tab.ActivitiesDetails))
        {
            tab.HasOnCampusActivity = false;
            return;
        }

        tab.HasOnCampusActivity = tab.ActivitiesDetails.Contains("campus", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Handles mapping between StateReport entity and StateReportForm DTO
/// </summary>
public static class StateReportFormMapper
{
    /// <summary>
    /// Maps StateReport entity to StateReportForm DTO for API responses
    /// </summary>
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

#endregion
