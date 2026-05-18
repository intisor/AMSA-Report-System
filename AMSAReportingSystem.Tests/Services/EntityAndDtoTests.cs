using AMSAReportingSystem.Data.Entities;
using AMSAReportingSystem.Services;

namespace AMSAReportingSystem.Tests.Services;

public sealed class EntityAndDtoTests
{
    [Fact]
    public void UnitPresidentAttendanceRate_WhenTotalIsZero_ReturnsZero()
    {
        var report = new StateReport { TotalUnitReportsCount = 0, UnitsAttendedTo = 5 };

        Assert.Equal(0m, report.UnitPresidentAttendanceRate);
    }

    [Fact]
    public void ReportWithStateContext_CanBeCreated()
    {
        var result = new ReportWithStateContext(
            1,
            2,
            3,
            "January 2025",
            ReportStatus.Draft,
            DateTime.UtcNow,
            DateTime.UtcNow,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            [],
            []);

        Assert.Equal(1, result.ReportId);
    }
}
