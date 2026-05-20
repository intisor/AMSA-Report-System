using AMSAReportingSystem.Data.Entities;
using AMSAReportingSystem.Services;

namespace AMSAReportingSystem.Tests.Services;

public sealed class StateReportFormMapperTests
{
    [Fact]
    public void ToForm_WhenStateReportProvided_MapsCoreProperties()
    {
        // Arrange
        var report = new StateReport
        {
            Id = 10,
            UnitsAttendedTo = 7,
            TotalUnitReportsCount = 10,
            UnitPerformanceRating = 85,
            UnitImprovementPlan = "Improve attendance",
            ChallengesFaced = "Transport",
            NationalSupportNeeded = "Funding",
            OtherNotes = "Notes",
            Programs =
            {
                new StateReportProgram { ProgramId = 2, ProgramName = "B", IsAutoAggregated = true },
                new StateReportProgram { ProgramId = 1, ProgramName = "A", IsAutoAggregated = false }
            }
        };

        // Act
        var result = StateReportFormMapper.ToForm(report);

        // Assert
        Assert.Equal(10, result.StateReportId);
        Assert.Equal(7, result.UnitPresidentsAttended);
        Assert.Equal(10, result.TotalUnitPresidents);
        Assert.Equal(2, result.Programs.Count);
        Assert.Equal("A", result.Programs[0].ProgramName);
    }
}
