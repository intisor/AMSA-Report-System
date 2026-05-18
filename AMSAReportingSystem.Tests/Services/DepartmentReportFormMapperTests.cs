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
    public void Deserialize_WhenTablighJsonProvided_ReturnsTablighForm()
    {
        var json = "{\"activitiesDetails\":\"Campus outreach program\"}";

        var result = DepartmentReportFormMapper.Deserialize(DepartmentType.Tabligh, json);

        Assert.IsType<TablighForm>(result);
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
    public void Serialize_WhenTablighFormHasNoActivities_ReturnsDerivedFlagFalse()
    {
        var form = new TablighForm();

        var json = DepartmentReportFormMapper.Serialize(DepartmentType.Tabligh, form);

        Assert.Contains("hasOnCampusActivity", json);
        Assert.Contains("false", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Deserialize_WhenInvalidJson_ReturnsDefaultFormInstance()
    {
        var result = DepartmentReportFormMapper.Deserialize(DepartmentType.Health, "not-json");

        Assert.IsType<HealthForm>(result);
    }
}
