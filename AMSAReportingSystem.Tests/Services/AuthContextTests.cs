using AMSAReportingSystem.Data.Entities;
using AMSAReportingSystem.Services;

namespace AMSAReportingSystem.Tests.Services;

public sealed class AuthContextTests
{
    [Fact]
    public void GetDisplayName_ReturnsFirstAndLastName()
    {
        var context = CreateContext();

        var result = context.GetDisplayName();

        Assert.Equal("Test User", result);
    }

    [Fact]
    public void GetDashboard_WhenNationalDashboardPresent_ReturnsNationalDashboard()
    {
        var context = CreateContext();
        context.Dashboards = new List<string> { "UnitDashboard", "NationalDashboard" };

        var result = context.GetDashboard();

        Assert.Equal("NationalDashboard", result);
    }

    [Fact]
    public void IsNationalLeadership_WhenNationalRoleExists_ReturnsTrue()
    {
        var context = CreateContext();
        context.ParsedRoles = new List<(string DepartmentName, LevelType LevelType)>
        {
            ("President", LevelType.National)
        };

        var result = context.IsNationalLeadership;

        Assert.True(result);
    }

    [Fact]
    public void HasSudoAccess_WhenNoLeadershipRoleExists_ReturnsFalse()
    {
        var context = CreateContext();
        context.ParsedRoles = new List<(string DepartmentName, LevelType LevelType)>
        {
            ("Taleem", LevelType.Unit)
        };

        var result = context.HasSudoAccess;

        Assert.False(result);
    }

    private static AuthContext CreateContext() => new()
    {
        MemberId = 1,
        MkanId = 1001,
        FirstName = "Test",
        LastName = "User",
        Email = "test@example.com",
        UnitId = 1,
        UnitName = "Unit",
        StateId = 1,
        StateName = "State",
        NationalId = 1,
        NationalName = "National",
        Token = "token",
        TokenExpiry = DateTime.UtcNow.AddHours(1),
        Roles = new List<string> { "Taleem:Unit" },
        ParsedRoles = new List<(string DepartmentName, LevelType LevelType)>()
    };
}
