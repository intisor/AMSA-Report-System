using AMSAReportingSystem.Data.Entities;
using System.Text.Json;

namespace AMSAReportingSystem.Services;

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

public static class DepartmentReportFormMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

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

    public static string Serialize(DepartmentType department, object form)
    {
        NormalizeDerivedFields(department, form);
        return JsonSerializer.Serialize(form, form.GetType(), JsonOptions);
    }

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
