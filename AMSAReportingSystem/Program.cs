using AMSAReportingSystem.Client.Pages;
using AMSAReportingSystem.Components;
using AMSAReportingSystem.Data;
using AMSAReportingSystem.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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

// Add typed HttpClient for AMSA API
builder.Services.AddHttpClient<IAmsaApiClient, AmsaApiClient>((serviceProvider, client) =>
	{
		var options = serviceProvider.GetRequiredService<IOptions<AmsaApiClientOptions>>();
		client.BaseAddress = new Uri(options.Value.BaseUrl);
		client.Timeout = TimeSpan.FromSeconds(options.Value.RequestTimeoutSeconds);
	});

// Add authentication services
// Authentication services

builder.Services.AddScoped<AmsaAuthService>();
builder.Services.AddScoped<AMSAAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<AMSAAuthStateProvider>());
builder.Services.AddSingleton<AMSAApiConnectionStatus>();

// API & Health services
// builder.Services.AddHostedService<AMSAApiStartupHealthCheckService>();

// Authorization & Report services
builder.Services.AddScoped<ReportAccessService>();
builder.Services.AddScoped<UnifiedReportService>();
builder.Services.AddScoped<CurrentUserReportService>();
builder.Services.AddScoped<AmsaDirectoryLookupCache>();

//     .AddRazorPages()
//     .AddApplicationInsightsTelemetry();

builder.Services.AddAuthorizationCore();

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

app.MapStaticAssets();
app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode()
	.AddInteractiveWebAssemblyRenderMode()
	.AddAdditionalAssemblies(typeof(AMSAReportingSystem.Client._Imports).Assembly);

app.Run();

public partial class Program;
