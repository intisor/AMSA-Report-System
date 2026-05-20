using AMSAReportingSystem.Data.Entities;
using AMSAReportingSystem.Services;

namespace AMSAReportingSystem.Tests.Services;

public sealed class ReportAccessServiceTests
{
    private readonly ReportAccessService _sut = new();

    [Fact]
    public void CanEditDepartment_WhenNationalLeadership_ReturnsTrue()
    {
        // Arrange
        var actor = CreateActor(LevelType.National, "President");

        // Act
        var result = _sut.CanEditDepartment(actor, unitId: 2, stateId: 3, DepartmentType.Taleem);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanEditDepartment_WhenStateLeadershipOnOwnState_ReturnsTrue()
    {
        // Arrange
        var actor = CreateActor(LevelType.State, "General Secretary", stateId: 10);

        // Act
        var result = _sut.CanEditDepartment(actor, unitId: 7, stateId: 10, DepartmentType.Welfare);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanEditDepartment_WhenUnitLeadershipOnOwnUnit_ReturnsTrue()
    {
        // Arrange
        var actor = CreateActor(LevelType.Unit, "President", unitId: 5, stateId: 10);

        // Act
        var result = _sut.CanEditDepartment(actor, unitId: 5, stateId: 3, DepartmentType.Sport);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanEditDepartment_WhenDepartmentMatchAtUnitLevel_ReturnsTrue()
    {
        // Arrange
        var actor = CreateActor(LevelType.Unit, "Taleem", unitId: 5, stateId: 10);

        // Act
        var result = _sut.CanEditDepartment(actor, unitId: 5, stateId: 10, DepartmentType.Taleem);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanEditDepartment_WhenNoRuleMatches_ReturnsFalse()
    {
        // Arrange
        var actor = CreateActor(LevelType.Unit, "Welfare", unitId: 5, stateId: 10);

        // Act
        var result = _sut.CanEditDepartment(actor, unitId: 9, stateId: 11, DepartmentType.Finance);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanInitiateReportSubmission_WhenNationalLeadership_ReturnsTrue()
    {
        // Arrange
        var actor = CreateActor(LevelType.National, "President");

        // Act
        var result = _sut.CanInitiateReportSubmission(actor, unitId: 20, stateId: 30);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanInitiateReportSubmission_WhenSameUnitAndStateWithUnitRole_ReturnsTrue()
    {
        // Arrange
        var actor = CreateActor(LevelType.Unit, "Taleem", unitId: 5, stateId: 10);

        // Act
        var result = _sut.CanInitiateReportSubmission(actor, unitId: 5, stateId: 10);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanInitiateReportSubmission_WhenOutsideScope_ReturnsFalse()
    {
        // Arrange
        var actor = CreateActor(LevelType.Unit, "Taleem", unitId: 5, stateId: 10);

        // Act
        var result = _sut.CanInitiateReportSubmission(actor, unitId: 7, stateId: 10);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanReviewAtUnitLevel_WhenUnitLeaderOnOwnUnit_ReturnsTrue()
    {
        // Arrange
        var actor = CreateActor(LevelType.Unit, "President", unitId: 5, stateId: 10);

        // Act
        var result = _sut.CanReviewAtUnitLevel(actor, unitId: 5);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanReviewAtStateLevel_WhenStateLeaderOnOwnState_ReturnsTrue()
    {
        // Arrange
        var actor = CreateActor(LevelType.State, "General Secretary", unitId: 5, stateId: 10);

        // Act
        var result = _sut.CanReviewAtStateLevel(actor, stateId: 10);

        // Assert
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
