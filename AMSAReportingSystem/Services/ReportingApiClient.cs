using System.Net.Http.Json;
using AMSAReportingSystem.Data.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace AMSAReportingSystem.Services;

public sealed class ReportingApiClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    private sealed record CreateDraftRequest(int CycleId);
    private sealed record DepartmentReportSaveRequest(string ReportDataJson, string? AdditionalNotes, bool MarkSubmitted);
    private sealed record ReportActionRequest(string? Notes);
    private sealed record StateReportSaveRequest(StateReportForm Form, bool MarkSubmitted);

    public async Task<ReportingCycle> GetActiveCycleAsync(CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync<ReportingCycle>("/api/reporting/active-cycle", ct)
            ?? throw new InvalidOperationException("Active cycle response was empty.");
    }

    public async Task<List<Report>> GetUnitReportsAsync(int? cycleId = null, CancellationToken ct = default)
    {
        var path = cycleId.HasValue
            ? $"/api/reporting/unit-reports?cycleId={cycleId.Value}"
            : "/api/reporting/unit-reports";

        return await _httpClient.GetFromJsonAsync<List<Report>>(path, ct) ?? [];
    }

    public async Task<Report?> GetCurrentDraftAsync(int cycleId, CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync<Report?>($"/api/reporting/current-draft?cycleId={cycleId}", ct);
    }

    public async Task<Report> CreateDraftAsync(int cycleId, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/reporting/current-draft", new CreateDraftRequest(cycleId), ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Report>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Create draft response was empty.");
    }

    public async Task<List<ReportRetrievalSummary>> GetReportHistoryAsync(CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync<List<ReportRetrievalSummary>>("/api/reporting/report-history", ct) ?? [];
    }

    public async Task<ReportRetrievalDetails?> GetReportDetailsAsync(int reportId, CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync<ReportRetrievalDetails?>($"/api/reporting/report-details/{reportId}", ct);
    }

    public async Task<Report> SubmitReportAsync(int reportId, string? notes = null, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/reporting/reports/{reportId}/submit", new ReportActionRequest(notes), ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Report>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Submit report response was empty.");
    }

    public async Task<DepartmentReport> SaveDepartmentAsync(int reportId, DepartmentType department, string reportDataJson, string? additionalNotes, bool markSubmitted, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/reporting/reports/{reportId}/departments/{department}/save", new DepartmentReportSaveRequest(reportDataJson, additionalNotes, markSubmitted), ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<DepartmentReport>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Department save response was empty.");
    }

    public async Task<ReportAttachment> AddDepartmentAttachmentAsync(int reportId, DepartmentType department, IBrowserFile file, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        using var content = new MultipartFormDataContent();
        await using var fileStream = file.OpenReadStream(10 * 1024 * 1024, ct);
        using var streamContent = new StreamContent(fileStream);
        if (!string.IsNullOrWhiteSpace(file.ContentType))
        {
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
        }

        content.Add(streamContent, "file", file.Name);

        var response = await _httpClient.PostAsync($"/api/reporting/reports/{reportId}/departments/{department}/attachments", content, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ReportAttachment>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Department attachment response was empty.");
    }

    public async Task RemoveDepartmentAttachmentAsync(int attachmentId, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync($"/api/reporting/reports/departments/attachments/{attachmentId}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<ReportWithStateContext>> GetNationalReportsAsync(int? cycleId = null, CancellationToken ct = default)
    {
        var path = cycleId.HasValue
            ? $"/api/reporting/national-reports?cycleId={cycleId.Value}"
            : "/api/reporting/national-reports";

        return await _httpClient.GetFromJsonAsync<List<ReportWithStateContext>>(path, ct) ?? [];
    }

    public async Task<List<Report>> GetStateReportsAsync(int? cycleId = null, CancellationToken ct = default)
    {
        var path = cycleId.HasValue
            ? $"/api/reporting/state-reports?cycleId={cycleId.Value}"
            : "/api/reporting/state-reports";

        return await _httpClient.GetFromJsonAsync<List<Report>>(path, ct) ?? [];
    }

    public async Task<StateReport> GetStateReportAsync(int cycleId, CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync<StateReport>($"/api/reporting/state-report?cycleId={cycleId}", ct)
            ?? throw new InvalidOperationException("State report response was empty.");
    }

    public async Task<StateReport> SaveStateReportAsync(int stateReportId, StateReportForm form, bool markSubmitted, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/reporting/state-report/{stateReportId}/save", new StateReportSaveRequest(form, markSubmitted), ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<StateReport>(cancellationToken: ct)
            ?? throw new InvalidOperationException("State report save response was empty.");
    }

    public async Task<StateReportAttachment> AddStateReportAttachmentAsync(int stateReportId, IBrowserFile file, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        using var content = new MultipartFormDataContent();
        await using var fileStream = file.OpenReadStream(10 * 1024 * 1024, ct);
        using var streamContent = new StreamContent(fileStream);
        if (!string.IsNullOrWhiteSpace(file.ContentType))
        {
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
        }

        content.Add(streamContent, "file", file.Name);

        var response = await _httpClient.PostAsync($"/api/reporting/state-report/{stateReportId}/attachments", content, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<StateReportAttachment>(cancellationToken: ct)
            ?? throw new InvalidOperationException("State attachment response was empty.");
    }

    public async Task RemoveStateReportAttachmentAsync(int attachmentId, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync($"/api/reporting/state-report/attachments/{attachmentId}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<Report> ApproveAtStateAsync(int reportId, string? notes = null, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/reporting/state-reports/{reportId}/approve", new ReportActionRequest(notes), ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Report>(cancellationToken: ct)
            ?? throw new InvalidOperationException("State approval response was empty.");
    }

    public async Task<Report> RejectAtStateAsync(int reportId, string? notes = null, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/reporting/state-reports/{reportId}/reject", new ReportActionRequest(notes), ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Report>(cancellationToken: ct)
            ?? throw new InvalidOperationException("State rejection response was empty.");
    }

    public async Task<Report> ApproveAtUnitAsync(int reportId, string? notes = null, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/reporting/unit-reports/{reportId}/approve", new ReportActionRequest(notes), ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Report>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Unit approval response was empty.");
    }

    public async Task<Report> RejectAtUnitAsync(int reportId, string? notes = null, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/reporting/unit-reports/{reportId}/reject", new ReportActionRequest(notes), ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Report>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Unit rejection response was empty.");
    }

    public async Task<Report> AcknowledgeAtNationalAsync(int reportId, string? notes = null, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/reporting/national-reports/{reportId}/acknowledge", new ReportActionRequest(notes), ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Report>(cancellationToken: ct)
            ?? throw new InvalidOperationException("National acknowledge response was empty.");
    }
}
