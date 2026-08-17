using LoanApproval.Application.Interfaces;
using LoanApproval.Application.Services;
using LoanApproval.Infrastructure.Data;
using LoanApproval.Infrastructure.Repositories;
using LoanApproval.Infrastructure.Services;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- EF Core / persistence -------------------------------------------------
//
// EnableRetryOnFailure installs SqlServerRetryingExecutionStrategy, which retries
// the transient SQL Server error numbers with exponential backoff. This matters
// far more against Azure SQL than LocalDB: throttling, and the brief connection
// drop during a failover, both present as transient errors that succeed on a
// second attempt. Without it, an ordinary request-time query surfaces them
// straight to the caller as a 500.
//
// Note this covers per-command faults during normal request handling. The startup
// path has its own coarser retry in DbSeeder, which wraps the whole
// migrate-and-seed sequence; the two compose, since a RetryLimitExceededException
// still carries the underlying SqlException as an inner exception.
//
// Caveat for later: a retrying strategy refuses user-initiated transactions
// (context.Database.BeginTransaction) unless the whole block is wrapped in
// strategy.ExecuteAsync. Nothing here does that today - the repository relies on
// SaveChangesAsync, which manages its own transaction - but it is the thing that
// breaks first if explicit transactions are added.
builder.Services.AddDbContext<LoanDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("LoanDb"),
        sqlServer => sqlServer.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));

// --- Dependency injection, with lifetimes chosen deliberately --------------
//
// Scoped: created once per HTTP request. Used for anything that wraps the
// DbContext (which is itself Scoped by AddDbContext), so a request sees a
// single consistent unit of work.
builder.Services.AddScoped<ILoanRepository, LoanRepository>();
builder.Services.AddScoped<ILoanApplicationService, LoanApplicationService>();
builder.Services.AddScoped<IApplicantService, ApplicantService>();

// Singleton: created once for the app's lifetime. Safe here because
// EligibilityService is stateless - it holds no per-request or per-user
// data, just constant thresholds, so there's no benefit to re-creating it.
builder.Services.AddSingleton<IEligibilityService, EligibilityService>();

// Transient: created every time it's requested. Used for the funding
// gateway and audit logger since they're cheap, side-effect-only calls
// with no shared state worth caching across a request.
builder.Services.AddTransient<IFundingGateway, MockFundingGateway>();
builder.Services.AddTransient<IAuditLogger, AuditLogger>();

// Serialize DecisionType as "Approved" rather than 0. The numeric form forces
// every client to hardcode the enum's ordinal, which then silently breaks if a
// member is ever inserted into the middle of the enum.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Applies pending migrations and seeds the demo applicants. This runs in every
// environment, including the deployed Azure Web App, so a fresh deployment comes
// up with a usable schema and demo data rather than an empty database. A scope is
// required here because LoanDbContext is Scoped and app.Services is the root provider.
using var scope = app.Services.CreateScope();
await DbSeeder.SeedAsync(
    scope.ServiceProvider.GetRequiredService<LoanDbContext>(),
    scope.ServiceProvider.GetRequiredService<ILogger<Program>>());

app.UseHttpsRedirection();

// Serves the built React app from wwwroot. In a deployed build the CI pipeline
// has already written Vite's output there; in local development wwwroot is
// typically empty because the Vite dev server on :5173 serves the UI instead.
app.UseStaticFiles();

app.UseAuthorization();
app.MapControllers();

// An unmatched /api/... path must 404 rather than fall through to the SPA
// fallback below, which would hand an API client the HTML shell with a 200.
// A catch-all route is the lowest-precedence match, so real controller routes
// still win over it.
app.Map("/api/{**path}", () => Results.NotFound());

// SPA fallback: anything else that matched neither a controller nor a static
// file returns index.html, so a hard refresh on a client-side route such as
// /members/M-1001 loads the app instead of 404ing.
app.MapFallbackToFile("index.html");

app.Run();

// Exposed for WebApplicationFactory in integration tests.
public partial class Program { }
