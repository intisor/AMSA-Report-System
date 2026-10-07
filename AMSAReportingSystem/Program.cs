using AMSAReportingSystem.Client.Pages;
using AMSAReportingSystem.Components;
using AMSAReportingSystem.Core.Abstractions;
using AMSAReportingSystem.Core.Storage;
using AMSAReportingSystem.Data;
using AMSAReportingSystem.Data.Entities;
using AMSAReportingSystem.Infrastructure.Storage;
using AMSAReportingSystem.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents()
	.AddInteractiveWebAssemblyComponents();

// Add database context
builder.Services.AddDbContext<AMSAReportingDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("AMSAReportingDb") 
		?? "Server=(localdb)\\mssqllocaldb;Database=AMSAReportingDb;Trusted_Connection=true;"));

// Configure AMSA API client options
builder.Services.Configure<AmsaApiClientOptions>(
	builder.Configuration.GetSection(AmsaApiClientOptions.SectionName));

// Add token cache singleton for AMSA API authentication
builder.Services.AddSingleton<AmsaTokenCache>();
builder.Services.Configure<LocalAttachmentStorageOptions>(builder.Configuration.GetSection("AttachmentStorage"));
builder.Services.AddScoped<IAttachmentStorage, LocalAttachmentStorage>();

// Add typed HttpClient for AMSA API
builder.Services.AddHttpClient<IAmsaApiClient, AmsaApiClient>((serviceProvider, client) =>
	{
		var options = serviceProvider.GetRequiredService<IOptions<AmsaApiClientOptions>>();
		client.BaseAddress = new Uri(options.Value.BaseUrl);
		client.Timeout = TimeSpan.FromSeconds(options.Value.RequestTimeoutSeconds);
	});

builder.Services.AddScoped(sp =>
{
	var navigationManager = sp.GetRequiredService<NavigationManager>();
	return new HttpClient
	{
		BaseAddress = new Uri(navigationManager.BaseUri)
	};
});
builder.Services.AddScoped<ReportingApiClient>();

// Add authentication services
// Authentication services

builder.Services.AddScoped<AmsaAuthService>();
builder.Services.AddScoped<AMSAAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<AMSAAuthStateProvider>());
builder.Services.AddScoped<ICurrentUserContext, AmsaCurrentUserContext>();
builder.Services.AddSingleton<AMSAApiConnectionStatus>();

// API & Health services
// builder.Services.AddHostedService<AMSAApiStartupHealthCheckService>();

// Authorization & Report services
builder.Services.AddScoped<ReportAccessService>();
builder.Services.AddScoped<UnifiedReportService>();
builder.Services.AddScoped<CurrentUserReportService>();
builder.Services.AddScoped<AmsaDirectoryLookupCache>();
builder.Services.AddScoped<IOrganizationDirectory>(sp => sp.GetRequiredService<AmsaDirectoryLookupCache>());

//     .AddRazorPages()
//     .AddApplicationInsightsTelemetry();

builder.Services.AddAuthorizationCore();
builder.Services.ConfigureHttpJsonOptions(options =>
{
	options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseWebAssemblyDebugging();
}
else
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapGet("/api/reporting/active-cycle", async Task<IResult> (
	CurrentUserReportService currentUserReportService,
	CancellationToken ct) =>
{
	try
	{
		var cycle = await currentUserReportService.EnsureActiveCycleAsync(ct);
		return Results.Ok(cycle);
	}
	catch (UnauthorizedAccessException)
	{
		return Results.Unauthorized();
	}
});

app.MapPost("/api/reporting/state-report/{stateReportId:int}/save", async Task<IResult> (
	int stateReportId,
	StateReportSaveRequest request,
	CurrentUserReportService currentUserReportService,
	CancellationToken ct) =>
{
	try
	{
		var stateReport = await currentUserReportService.SaveMyStateReportAsync(stateReportId, request.Form, request.MarkSubmitted, ct);
		return Results.Ok(stateReport);
	}
	catch (UnauthorizedAccessException)
	{
		return Results.Unauthorized();
	}
	catch (InvalidOperationException ex)
	{
		return Results.BadRequest(new { error = ex.Message });
	}
});

app.MapPost("/api/reporting/state-report/{stateReportId:int}/attachments", async Task<IResult> (
	int stateReportId,
	HttpRequest request,
	CurrentUserReportService currentUserReportService,
	CancellationToken ct) =>
{
	try
	{
		if (!request.HasFormContentType)
		{
			return Results.BadRequest(new { error = "Multipart form data is required." });
		}

		var form = await request.ReadFormAsync(ct);
		var file = form.Files["file"];
		if (file is null || file.Length == 0)
		{
			return Results.BadRequest(new { error = "Attachment file is required." });
		}

		await using var stream = file.OpenReadStream();
		using var memory = new MemoryStream();
		await stream.CopyToAsync(memory, ct);
		var attachment = await currentUserReportService.AddStateReportAttachmentAsync(stateReportId, file.FileName, file.ContentType, memory.ToArray(), ct);
		return Results.Ok(attachment);
	}
	catch (UnauthorizedAccessException)
	{
		return Results.Unauthorized();
	}
	catch (InvalidOperationException ex)
	{
		return Results.BadRequest(new { error = ex.Message });
	}
});

app.MapDelete("/api/reporting/state-report/attachments/{attachmentId:int}", async Task<IResult> (
	int attachmentId,
	CurrentUserReportService currentUserReportService,
	CancellationToken ct) =>
{
	try
	{
		await currentUserReportService.RemoveStateReportAttachmentAsync(attachmentId, ct);
		return Results.NoContent();
	}
	catch (UnauthorizedAccessException)
	{
		return Results.Unauthorized();
	}
	catch (InvalidOperationException ex)
	{
		return Results.BadRequest(new { error = ex.Message });
	}
});

app.MapPost("/api/reporting/state-reports/{reportId:int}/approve", async Task<IResult> (
	int reportId,
	ReportActionRequest request,
	CurrentUserReportService currentUserReportService,
	CancellationToken ct) =>
{
	try
	{
		var report = await currentUserReportService.ApproveAtStateAsync(reportId, request.Notes, ct);
		return Results.Ok(report);
	}
	catch (UnauthorizedAccessException)
	{
		return Results.Unauthorized();
	}
	catch (InvalidOperationException ex)
	{
		return Results.BadRequest(new { error = ex.Message });
	}
});

app.MapPost("/api/reporting/state-reports/{reportId:int}/reject", async Task<IResult> (
	int reportId,
	ReportActionRequest request,
	CurrentUserReportService currentUserReportService,
	CancellationToken ct) =>
{
	try
	{
		var report = await currentUserReportService.RejectAtStateAsync(reportId, request.Notes, ct);
		return Results.Ok(report);
	}
	catch (UnauthorizedAccessException)
	{
		return Results.Unauthorized();
	}
	catch (InvalidOperationException ex)
	{
		return Results.BadRequest(new { error = ex.Message });
	}
});

app.MapPost("/api/reporting/unit-reports/{reportId:int}/approve", async Task<IResult> (
	int reportId,
	ReportActionRequest request,
	CurrentUserReportService currentUserReportService,
	CancellationToken ct) =>
{
	try
	{
		var report = await currentUserReportService.ApproveAtUnitAsync(reportId, request.Notes, ct);
		return Results.Ok(report);
	}
	catch (UnauthorizedAccessException)
	{
		return Results.Unauthorized();
	}
	catch (InvalidOperationException ex)
	{
		return Results.BadRequest(new { error = ex.Message });
	}
});

app.MapPost("/api/reporting/unit-reports/{reportId:int}/reject", async Task<IResult> (
	int reportId,
	ReportActionRequest request,
	CurrentUserReportService currentUserReportService,
	CancellationToken ct) =>
{
	try
	{
		var report = await currentUserReportService.RejectAtUnitAsync(reportId, request.Notes, ct);
		return Results.Ok(report);
	}
	catch (UnauthorizedAccessException)
	{
		return Results.Unauthorized();
	}
	catch (InvalidOperationException ex)
	{
		return Results.BadRequest(new { error = ex.Message });
	}
});

app.MapGet("/api/reporting/unit-reports", async Task<IResult> (
	int? cycleId,
	CurrentUserReportService currentUserReportService,
	CancellationToken ct) =>
{
	try
	{
		var reports = await currentUserReportService.GetMyUnitReportsAsync(cycleId, ct);
		return Results.Ok(reports);
	}
	catch (UnauthorizedAccessException)
	{
		return Results.Unauthorized();
	}
});

app.MapGet("/api/reporting/current-draft", async Task<IResult> (
	int cycleId,
	CurrentUserReportService currentUserReportService,
	CancellationToken ct) =>
{
	try
	{
		var report = await currentUserReportService.GetCurrentUserDraftAsync(cycleId, ct);
		return Results.Ok(report);
	}
	catch (UnauthorizedAccessException)
	{
		return Results.Unauthorized();
	}
});

app.MapPost("/api/reporting/current-draft", async Task<IResult> (
	CreateDraftRequest request,
	CurrentUserReportService currentUserReportService,
	CancellationToken ct) =>
{
	try
	{
		var report = await currentUserReportService.CreateCurrentUserDraftAsync(request.CycleId, ct);
		return Results.Ok(report);
	}
	catch (UnauthorizedAccessException)
	{
		return Results.Unauthorized();
	}
	catch (InvalidOperationException ex)
	{
		return Results.BadRequest(new { error = ex.Message });
	}
});

app.MapPost("/api/reporting/reports/{reportId:int}/submit", async Task<IResult> (
	int reportId,
	ReportActionRequest request,
	CurrentUserReportService currentUserReportService,
	CancellationToken ct) =>
{
	try
	{
		var report = await currentUserReportService.SubmitReportToPresidentAsync(reportId, request.Notes, ct);
		return Results.Ok(report);
	}
	catch (UnauthorizedAccessException)
	{
		return Results.Unauthorized();
	}
	catch (InvalidOperationException ex)
	{
		return Results.BadRequest(new { error = ex.Message });
	}
});

app.MapPost("/api/reporting/reports/{reportId:int}/departments/{department}/save", async Task<IResult> (
	int reportId,
	string department,
	DepartmentReportSaveRequest request,
	CurrentUserReportService currentUserReportService,
	CancellationToken ct) =>
{
	try
	{
		if (!Enum.TryParse<DepartmentType>(department, true, out var departmentType))
		{
			return Results.BadRequest(new { error = $"Unknown department '{department}'." });
		}

		var result = await currentUserReportService.SaveDepartmentJsonAsync(reportId, departmentType, request.ReportDataJson, request.AdditionalNotes, request.MarkSubmitted, ct);
		return Results.Ok(result);
	}
	catch (UnauthorizedAccessException)
	{
		return Results.Unauthorized();
	}
	catch (ArgumentException ex)
	{
		return Results.BadRequest(new { error = ex.Message });
	}
	catch (InvalidOperationException ex)
	{
		return Results.BadRequest(new { error = ex.Message });
	}
});

app.MapPost("/api/reporting/reports/{reportId:int}/departments/{department}/attachments", async Task<IResult> (
	int reportId,
	string department,
	HttpRequest request,
	CurrentUserReportService currentUserReportService,
	CancellationToken ct) =>
{
	try
	{
		if (!Enum.TryParse<DepartmentType>(department, true, out var departmentType))
		{
			return Results.BadRequest(new { error = $"Unknown department '{department}'." });
		}

		if (!request.HasFormContentType)
		{
			return Results.BadRequest(new { error = "Multipart form data is required." });
		}

		var form = await request.ReadFormAsync(ct);
		var file = form.Files["file"];
		if (file is null || file.Length == 0)
		{
			return Results.BadRequest(new { error = "Attachment file is required." });
		}

		await using var stream = file.OpenReadStream();
		using var memory = new MemoryStream();
		await stream.CopyToAsync(memory, ct);
		var attachment = await currentUserReportService.AddDepartmentAttachmentAsync(reportId, departmentType, file.FileName, file.ContentType, memory.ToArray(), ct);
		return Results.Ok(attachment);
	}
	catch (UnauthorizedAccessException)
	{
		return Results.Unauthorized();
	}
	catch (InvalidOperationException ex)
	{
		return Results.BadRequest(new { error = ex.Message });
	}
});

app.MapDelete("/api/reporting/reports/departments/attachments/{attachmentId:int}", async Task<IResult> (
	int attachmentId,
	CurrentUserReportService currentUserReportService,
	CancellationToken ct) =>
{
	try
	{
		await currentUserReportService.RemoveDepartmentAttachmentAsync(attachmentId, ct);
		return Results.NoContent();
	}
	catch (UnauthorizedAccessException)
	{
		return Results.Unauthorized();
	}
	catch (InvalidOperationException ex)
	{
		return Results.BadRequest(new { error = ex.Message });
	}
});

app.MapGet("/api/reporting/report-history", async Task<IResult> (
	CurrentUserReportService currentUserReportService,
	CancellationToken ct) =>
{
	try
	{
		var history = await currentUserReportService.GetCurrentUserReportHistoryAsync(ct);
		return Results.Ok(history);
	}
	catch (UnauthorizedAccessException)
	{
		return Results.Unauthorized();
	}
});

app.MapGet("/api/reporting/report-details/{reportId:int}", async Task<IResult> (
	int reportId,
	CurrentUserReportService currentUserReportService,
	CancellationToken ct) =>
{
	try
	{
		var details = await currentUserReportService.GetReportDetailsAsync(reportId, ct);
		return details is null ? Results.NotFound() : Results.Ok(details);
	}
	catch (UnauthorizedAccessException)
	{
		return Results.Unauthorized();
	}
	catch (InvalidOperationException ex)
	{
		return Results.BadRequest(new { error = ex.Message });
	}
});

app.MapGet("/api/reporting/state-reports", async Task<IResult> (
	int? cycleId,
	CurrentUserReportService currentUserReportService,
	CancellationToken ct) =>
{
	try
	{
		var reports = await currentUserReportService.GetStateUnitReportsAsync(cycleId, ct);
		return Results.Ok(reports);
	}
	catch (UnauthorizedAccessException)
	{
		return Results.Unauthorized();
	}
});

app.MapGet("/api/reporting/state-report", async Task<IResult> (
	int cycleId,
	CurrentUserReportService currentUserReportService,
	CancellationToken ct) =>
{
	try
	{
		var stateReport = await currentUserReportService.EnsureMyStateReportAsync(cycleId, ct);
		return Results.Ok(stateReport);
	}
	catch (UnauthorizedAccessException)
	{
		return Results.Unauthorized();
	}
	catch (InvalidOperationException ex)
	{
		return Results.BadRequest(new { error = ex.Message });
	}
});

app.MapGet("/api/reporting/national-reports", async Task<IResult> (
	int? cycleId,
	CurrentUserReportService currentUserReportService,
	CancellationToken ct) =>
{
	try
	{
		var reports = await currentUserReportService.GetNationalReportsWithStateContextAsync(cycleId, ct);
		return Results.Ok(reports);
	}
	catch (UnauthorizedAccessException)
	{
		return Results.Unauthorized();
	}
});

app.MapPost("/api/reporting/national-reports/{reportId:int}/acknowledge", async Task<IResult> (
	int reportId,
	ReportActionRequest request,
	CurrentUserReportService currentUserReportService,
	CancellationToken ct) =>
{
	try
	{
		var report = await currentUserReportService.AcknowledgeAtNationalAsync(reportId, request.Notes, ct);
		return Results.Ok(report);
	}
	catch (UnauthorizedAccessException)
	{
		return Results.Unauthorized();
	}
	catch (InvalidOperationException ex)
	{
		return Results.BadRequest(new { error = ex.Message });
	}
});

app.MapGet("/attachments/department/{attachmentId:int}", async Task<IResult> (
	int attachmentId,
	CurrentUserReportService currentUserReportService,
	IAttachmentStorage attachmentStorage,
	CancellationToken ct) =>
{
	try
	{
		var attachment = await currentUserReportService.GetDepartmentAttachmentAsync(attachmentId, ct);
		var file = await attachmentStorage.OpenReadAsync(attachment.StoredFileName, attachment.ContentType, ct);
		return Results.File(file.Content, file.ContentType, attachment.FileName);
	}
	catch (UnauthorizedAccessException)
	{
		return Results.Unauthorized();
	}
	catch (InvalidOperationException)
	{
		return Results.NotFound();
	}
	catch (FileNotFoundException)
	{
		return Results.NotFound();
	}
});

app.MapGet("/attachments/state/{attachmentId:int}", async Task<IResult> (
	int attachmentId,
	CurrentUserReportService currentUserReportService,
	IAttachmentStorage attachmentStorage,
	CancellationToken ct) =>
{
	try
	{
		var attachment = await currentUserReportService.GetStateReportAttachmentAsync(attachmentId, ct);
		var file = await attachmentStorage.OpenReadAsync(attachment.StoredFileName, attachment.ContentType, ct);
		return Results.File(file.Content, file.ContentType, attachment.FileName);
	}
	catch (UnauthorizedAccessException)
	{
		return Results.Unauthorized();
	}
	catch (InvalidOperationException)
	{
		return Results.NotFound();
	}
	catch (FileNotFoundException)
	{
		return Results.NotFound();
	}
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode()
	.AddInteractiveWebAssemblyRenderMode()
	.AddAdditionalAssemblies(typeof(AMSAReportingSystem.Client._Imports).Assembly);

app.Run();

public partial class Program;

internal sealed record ReportActionRequest(string? Notes);
internal sealed record CreateDraftRequest(int CycleId);
internal sealed record DepartmentReportSaveRequest(string ReportDataJson, string? AdditionalNotes, bool MarkSubmitted);
internal sealed record StateReportSaveRequest(StateReportForm Form, bool MarkSubmitted);
