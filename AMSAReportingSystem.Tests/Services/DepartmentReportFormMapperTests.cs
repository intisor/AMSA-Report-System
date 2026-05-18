using AMSAReportingSystem.Data.Entities;
using AMSAReportingSystem.Services;

namespace AMSAReportingSystem.Tests.Services;

public sealed class DepartmentReportFormMapperTests
{
    [Fact]
    public void Deserialize_WhenJsonIsEmpty_ReturnsDefaultTaleemForm()
    {
        var result = DepartmentReportFormMapper.Deserialize(DepartmentType.Taleem, null);

        Assert.IsType<TaleemForm>(result);
    }

    [Fact]
    public void Deserialize_WhenTablighJsonContainsCampus_ReturnsCampusFlagTrue()
    {
        var json = "{\"activitiesDetails\":\"Campus outreach program\"}";

        var result = DepartmentReportFormMapper.Deserialize(DepartmentType.Tabligh, json);

        var form = Assert.IsType<TablighForm>(result);
        Assert.True(form.HasOnCampusActivity);
    }

    [Fact]
    public void Serialize_WhenTablighFormHasCampusText_PersistsDerivedFlag()
    {
        var form = new TablighForm { ActivitiesDetails = "Campus outreach program" };

        var json = DepartmentReportFormMapper.Serialize(DepartmentType.Tabligh, form);

        Assert.Contains("hasOnCampusActivity", json);
        Assert.Contains("true", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Deserialize_WhenInvalidJson_ReturnsDefaultFormInstance()
    {
        var result = DepartmentReportFormMapper.Deserialize(DepartmentType.Health, "not-json");

        Assert.IsType<HealthForm>(result);
    }
}
