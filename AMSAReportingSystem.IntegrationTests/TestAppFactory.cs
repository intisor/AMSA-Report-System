using AMSAReportingSystem.Core.Abstractions;
using AMSAReportingSystem.Data;
using AMSAReportingSystem.Data.Entities;
using AMSAReportingSystem.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace AMSAReportingSystem.IntegrationTests;

public sealed class TestAppFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public WebApplicationFactory<Program> WithoutCurrentUser()
    {
        return WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll(typeof(ICurrentUserContext));
                services.AddScoped<ICurrentUserContext, NoCurrentUserContext>();
            });
        });
    }

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        _connection.Open();

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<AMSAReportingDbContext>));
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<AMSAReportingDbContext>));
            services.RemoveAll(typeof(AMSAReportingDbContext));
            services.RemoveAll(typeof(ICurrentUserContext));
            services.RemoveAll(typeof(IAmsaApiClient));

            services.AddEntityFrameworkSqlite();
            services.AddDbContext<AMSAReportingDbContext>(options =>
            {
                options.UseSqlite(_connection);
                options.EnableSensitiveDataLogging();
            });
            services.AddScoped<ICurrentUserContext, TestCurrentUserContext>();
            services.AddSingleton<IAmsaApiClient, TestAmsaApiClient>();
        });

        builder.ConfigureTestServices(services =>
        {
            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AMSAReportingDbContext>();
            db.Database.EnsureCreated();
            SeedReportingData(db);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }

    private sealed class TestCurrentUserContext : ICurrentUserContext
    {
        private static readonly CurrentUserScope User = new(
            MemberId: 1001,
            UnitId: 10,
            StateId: 20,
            NationalId: 30,
            Roles:
            [
                new RoleScope("President", OrganizationLevel.Unit),
                new RoleScope("President", OrganizationLevel.State),
                new RoleScope("President", OrganizationLevel.National),
                new RoleScope("General", OrganizationLevel.Unit)
            ],
            HasSudoAccess: true);

        public ValueTask<CurrentUserScope?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult<CurrentUserScope?>(User);
        }
    }

    private sealed class NoCurrentUserContext : ICurrentUserContext
    {
        public ValueTask<CurrentUserScope?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult<CurrentUserScope?>(null);
        }
    }

    private sealed class TestAmsaApiClient : IAmsaApiClient
    {
        public Task<Result<TokenResponse>> GenerateTokenAsync(string mkanId, IEnumerable<string> requestedScopes, CancellationToken ct = default) =>
            Task.FromResult(Result<TokenResponse>.Success(new TokenResponse { Token = "test-token", ExpiresAt = DateTime.UtcNow.AddHours(1) }));

        public Task<Result<MemberResponse>> GetMemberByIdAsync(int memberId, CancellationToken ct = default) =>
            Task.FromResult(Result<MemberResponse>.Failure("Not needed in integration tests."));

        public Task<Result<MemberResponse>> GetMemberByMkanAsync(string mkanId, CancellationToken ct = default) =>
            Task.FromResult(Result<MemberResponse>.Failure("Not needed in integration tests."));

        public Task<Result<List<StateResponse>>> GetAllStatesAsync(CancellationToken ct = default) =>
            Task.FromResult(Result<List<StateResponse>>.Success([]));

        public Task<Result<StateResponse>> GetStateByIdAsync(int stateId, CancellationToken ct = default) =>
            Task.FromResult(Result<StateResponse>.Success(new StateResponse
            {
                StateId = stateId,
                StateName = $"State {stateId}",
                UnitCount = 1,
                MemberCount = 10,
                ExcoCount = 2
            }));

        public Task<Result<List<UnitResponse>>> GetUnitsByStateAsync(int stateId, CancellationToken ct = default) =>
            Task.FromResult(Result<List<UnitResponse>>.Success([]));

        public Task<Result<UnitResponse>> GetUnitByIdAsync(int unitId, CancellationToken ct = default) =>
            Task.FromResult(Result<UnitResponse>.Success(new UnitResponse
            {
                UnitId = unitId,
                UnitName = $"Unit {unitId}",
                StateId = 20,
                StateName = "State 20",
                MemberCount = 10,
                ExcoCount = 2,
                Members = []
            }));

        public Task<Result<AppRegistrationResponse>> CreateAppAsync(CreateAppRequest request, CancellationToken ct = default) =>
            Task.FromResult(Result<AppRegistrationResponse>.Failure("Not needed in integration tests."));

        public Task<Result<AppRegistrationResponse>> GetAppAsync(string appId, CancellationToken ct = default) =>
            Task.FromResult(Result<AppRegistrationResponse>.Failure("Not needed in integration tests."));
    }

    private static void SeedReportingData(AMSAReportingDbContext db)
    {
        db.ReportActivityLogs.RemoveRange(db.ReportActivityLogs);
        db.ReportAttachments.RemoveRange(db.ReportAttachments);
        db.DepartmentReports.RemoveRange(db.DepartmentReports);
        db.StateReportAttachments.RemoveRange(db.StateReportAttachments);
        db.StateReportPrograms.RemoveRange(db.StateReportPrograms);
        db.StateReports.RemoveRange(db.StateReports);
        db.Reports.RemoveRange(db.Reports);
        db.ReportingCycles.RemoveRange(db.ReportingCycles);
        db.SaveChanges();

        var now = DateTime.UtcNow;
        var cycle = new ReportingCycle
        {
            Id = 100,
            CycleMonth = now.ToString("MMMM yyyy"),
            StartDate = new DateTime(now.Year, now.Month, 1),
            EndDate = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month)),
            SubmissionDeadline = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month)).AddDays(7),
            IsLocked = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        var report = new Report
        {
            Id = 200,
            UnitId = 10,
            StateId = 20,
            CycleId = cycle.Id,
            Status = ReportStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now,
            DepartmentReports = new List<DepartmentReport>
            {
                new()
                {
                    Id = 300,
                    CycleId = cycle.Id,
                    Department = DepartmentType.General,
                    ReportData = "{}",
                    AdditionalNotes = "General note",
                    IsSubmitted = true,
                    SubmittedAt = now,
                    SubmittedByMemberId = 1001,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new()
                {
                    Id = 301,
                    CycleId = cycle.Id,
                    Department = DepartmentType.Taleem,
                    ReportData = "{}",
                    IsSubmitted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                }
            },
            ActivityLogs = new List<ReportActivityLog>
            {
                new()
                {
                    Id = 400,
                    ActionByMemberId = 1001,
                    Action = "ReportCreated",
                    ActionAt = now
                }
            }
        };

        db.ReportingCycles.Add(cycle);
        db.Reports.Add(report);
        db.SaveChanges();
    }
}
