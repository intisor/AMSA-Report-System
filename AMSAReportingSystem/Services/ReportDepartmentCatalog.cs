using AMSAReportingSystem.Data.Entities;

namespace AMSAReportingSystem.Services;

public static class ReportDepartmentCatalog
{
    public static IReadOnlyList<DepartmentType> ReportableDepartments { get; } = new[]
    {
        DepartmentType.Taleem,
        DepartmentType.Tabligh,
        DepartmentType.Welfare,
        DepartmentType.Sport,
        DepartmentType.Finance,
        DepartmentType.Health,
        DepartmentType.SecondarySchool,
        DepartmentType.Tajneed,
        DepartmentType.General
    };
}
