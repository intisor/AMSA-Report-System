using AMSAReportingSystem.Data.Entities;
using AMSAReportingSystem.Services;

namespace AMSAReportingSystem.Tests.Services;

public sealed class ReportDepartmentCatalogTests
{
    [Fact]
    public void ReportableDepartments_ContainsAllDepartmentsInExpectedOrder()
    {
        var result = ReportDepartmentCatalog.ReportableDepartments;

        Assert.Equal(new[]
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
        }, result);
    }
}
