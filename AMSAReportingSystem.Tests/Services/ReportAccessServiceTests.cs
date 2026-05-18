using AMSAReportingSystem.Data.Entities;
using AMSAReportingSystem.Services;

namespace AMSAReportingSystem.Tests.Services;

public sealed class ReportAccessServiceTests
{
    private readonly ReportAccessService _sut = new();

    [Fact]
    public void CanEditDepartment_WhenNationalLeadership_ReturnsTrue()
    {
        var actor = CreateActor(LevelType.National, "President");

        var result = _sut.CanEditDepartment(actor, unitId: 2, stateId: 3, DepartmentType.Taleem);

        Assert.True(result);
    }

    [Fact]
    public void CanEditDepartment_WhenStateLeadershipOnOwnState_ReturnsTrue()
    {
        var actor = CreateActor(LevelType.State, "General Secretary", stateId: 10);

        var result = _sut.CanEditDepartment(actor, unitId: 7, stateId: 10, DepartmentType.Welfare);

        Assert.True(result);
    }

    [Fact]
    public void CanEditDepartment_WhenUnitLeadershipOnOwnUnit_ReturnsTrue()
    {
        var actor = CreateActor(LevelType.Unit, "President", unitId: 5, stateId: 10);

        var result = _sut.CanEditDepartment(actor, unitId: 5, stateId: 3, DepartmentType.Sport);

        Assert.True(result);
    }

    [Fact]
    public void CanEditDepartment_WhenDepartmentMatchAtUnitLevel_ReturnsTrue()
    {
        var actor = CreateActor(LevelType.Unit, "Taleem", unitId: 5, stateId: 10);

        var result = _sut.CanEditDepartment(actor, unitId: 5, stateId: 10, DepartmentType.Taleem);

        Assert.True(result);
    }

    [Fact]
    public void CanEditDepartment_WhenNoRuleMatches_ReturnsFalse()
    {
        var actor = CreateActor(LevelType.Unit, "Welfare", unitId: 5, stateId: 10);

        var result = _sut.CanEditDepartment(actor, unitId: 9, stateId: 11, DepartmentType.Finance);

        Assert.False(result);
    }

    [Fact]
    public void CanInitiateReportSubmission_WhenNationalLeadership_ReturnsTrue()
    {
        var actor = CreateActor(LevelType.National, "President");

        var result = _sut.CanInitiateReportSubmission(actor, unitId: 20, stateId: 30);

        Assert.True(result);
    }

    [Fact]
    public void CanInitiateReportSubmission_WhenSameUnitAndStateWithUnitRole_ReturnsTrue()
    {
        var actor = CreateActor(LevelType.Unit, "Taleem", unitId: 5, stateId: 10);

        var result = _sut.CanInitiateReportSubmission(actor, unitId: 5, stateId: 10);

        Assert.True(result);
    }

    [Fact]
    public void CanInitiateReportSubmission_WhenOutsideScope_ReturnsFalse()
    {
        var actor = CreateActor(LevelType.Unit, "Taleem", unitId: 5, stateId: 10);

        var result = _sut.CanInitiateReportSubmission(actor, unitId: 7, stateId: 10);

        Assert.False(result);
    }

    [Fact]
    public void CanReviewAtUnitLevel_WhenUnitLeaderOnOwnUnit_ReturnsTrue()
    {
        var actor = CreateActor(LevelType.Unit, "President", unitId: 5, stateId: 10);

        var result = _sut.CanReviewAtUnitLevel(actor, unitId: 5);

        Assert.True(result);
    }

    [Fact]
    public void CanReviewAtStateLevel_WhenStateLeaderOnOwnState_ReturnsTrue()
    {
        var actor = CreateActor(LevelType.State, "General Secretary", unitId: 5, stateId: 10);

        var result = _sut.CanReviewAtStateLevel(actor, stateId: 10);

        Assert.True(result);
    }

    private static AuthContext CreateActor(LevelType levelType, string departmentName, int unitId = 1, int stateId = 1)
    {
        return new AuthContext
        {
            MemberId = 1,
            MkanId = 1001,
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            UnitId = unitId,
            UnitName = "Unit",
            StateId = stateId,
            StateName = "State",
            NationalId = 1,
            NationalName = "National",
            Token = "token",
            TokenExpiry = DateTime.UtcNow.AddHours(1),
            Roles = new List<string> { $"{departmentName}:{levelType}" },
            ParsedRoles = new List<(string DepartmentName, LevelType LevelType)> { (departmentName, levelType) }
        };
    }
}
