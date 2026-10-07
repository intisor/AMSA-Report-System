using System.Net.Http.Json;
using AMSAReportingSystem.Data.Entities;
using AMSAReportingSystem.Services;

namespace AMSAReportingSystem.IntegrationTests;

public sealed class ReportingApiSuccessTests : IClassFixture<TestAppFactory>
{
    private readonly TestAppFactory _factory;

    public ReportingApiSuccessTests(TestAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCurrentDraft_ReturnsSeededDraftReport()
    {
        var client = _factory.CreateClient();
        var cycle = await client.GetFromJsonAsync<ReportingCycle>("/api/reporting/active-cycle");

        var response = await client.GetAsync($"/api/reporting/current-draft?cycleId={cycle!.Id}");

        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<Report>();
        Assert.NotNull(report);
        Assert.Equal(10, report.UnitId);
        Assert.Equal(ReportStatus.Draft, report.Status);
    }

    [Fact]
    public async Task GetReportHistory_ReturnsSeededHistory()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/reporting/report-history");

        response.EnsureSuccessStatusCode();
        var history = await response.Content.ReadFromJsonAsync<List<ReportRetrievalSummary>>();
        Assert.NotNull(history);
        Assert.NotEmpty(history);
    }

    [Fact]
    public async Task GetReportDetails_ReturnsSeededDetails()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/reporting/report-details/200");

        response.EnsureSuccessStatusCode();
        var details = await response.Content.ReadFromJsonAsync<ReportRetrievalDetails>();
        Assert.NotNull(details);
        Assert.Equal(200, details.ReportId);
    }

    [Fact]
    public async Task SubmitReport_ReturnsUpdatedStatus()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/reporting/reports/200/submit", new { Notes = "Integration submit" });

        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<Report>();
        Assert.NotNull(report);
        Assert.Equal(ReportStatus.SubmittedToPresident, report.Status);
    }
}
