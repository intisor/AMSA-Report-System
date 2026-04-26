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
builder.Services.AddDbContext<AmsaReportingDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("AmsaReportingDb") 
		?? "Server=(localdb)\\mssqllocaldb;Database=AmsaReportingDb;Trusted_Connection=true;"));

// Configure AMSA API client options
builder.Services.Configure<AmSaApiClientOptions>(
	builder.Configuration.GetSection(AmSaApiClientOptions.SectionName));

// Add typed HttpClient for AMSA API
builder.Services.AddHttpClient<IAmSaApiClient, AmSaApiClient>((serviceProvider, client) =>
	{
		var options = serviceProvider.GetRequiredService<IOptions<AmSaApiClientOptions>>();
		client.BaseAddress = new Uri(options.Value.BaseUrl);
		client.Timeout = TimeSpan.FromSeconds(options.Value.RequestTimeoutSeconds);
	});

// Add authentication services
builder.Services.AddScoped<AmSaAuthService>();
builder.Services.AddScoped<AmsaAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<AmsaAuthStateProvider>());
builder.Services.AddSingleton<AmsaApiConnectionStatus>();
builder.Services.AddScoped<ReportAccessService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<CurrentUserReportService>();
builder.Services.AddHostedService<AmsaApiStartupHealthCheckService>();
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
