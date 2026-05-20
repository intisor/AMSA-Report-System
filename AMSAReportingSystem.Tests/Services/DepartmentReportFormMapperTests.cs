using AMSAReportingSystem.Data.Entities;
using AMSAReportingSystem.Services;

namespace AMSAReportingSystem.Tests.Services;

public sealed class DepartmentReportFormMapperTests
{
    [Fact]
    public void Deserialize_WhenJsonIsEmpty_ReturnsDefaultTaleemForm()
    {
        // Arrange & Act
        var result = DepartmentReportFormMapper.Deserialize(DepartmentType.Taleem, null);

        // Assert
        Assert.IsType<TaleemForm>(result);
    }

    [Fact]
    public void Deserialize_WhenTablighJsonProvided_ReturnsTablighForm()
    {
        // Arrange
        var json = "{\"activitiesDetails\":\"Campus outreach program\"}";

        // Act
        var result = DepartmentReportFormMapper.Deserialize(DepartmentType.Tabligh, json);

        // Assert
        Assert.IsType<TablighForm>(result);
    }

    [Fact]
    public void Serialize_WhenTablighFormHasCampusText_PersistsDerivedFlag()
    {
        // Arrange
        var form = new TablighForm { ActivitiesDetails = "Campus outreach program" };

        // Act
        var json = DepartmentReportFormMapper.Serialize(DepartmentType.Tabligh, form);

        // Assert
        Assert.Contains("hasOnCampusActivity", json);
        Assert.Contains("true", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Serialize_WhenTablighFormHasNoActivities_ReturnsDerivedFlagFalse()
    {
        // Arrange
        var form = new TablighForm();

        // Act
        var json = DepartmentReportFormMapper.Serialize(DepartmentType.Tabligh, form);

        // Assert
        Assert.Contains("hasOnCampusActivity", json);
        Assert.Contains("false", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Deserialize_WhenInvalidJson_ReturnsDefaultFormInstance()
    {
        // Arrange & Act
        var result = DepartmentReportFormMapper.Deserialize(DepartmentType.Health, "not-json");

        // Assert
        Assert.IsType<HealthForm>(result);
    }
}
