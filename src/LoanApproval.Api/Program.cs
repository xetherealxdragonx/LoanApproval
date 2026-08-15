using LoanApproval.Application.Interfaces;
using LoanApproval.Application.Services;
using LoanApproval.Infrastructure.Data;
using LoanApproval.Infrastructure.Repositories;
using LoanApproval.Infrastructure.Services;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- EF Core / persistence -------------------------------------------------
builder.Services.AddDbContext<LoanDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("LoanDb")));

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

    // Development only: apply migrations and seed demo applicants so the API is
    // immediately demoable against an empty database. A scope is required here
    // because LoanDbContext is Scoped and app.Services is the root provider.
    using var scope = app.Services.CreateScope();
    await DbSeeder.SeedAsync(
        scope.ServiceProvider.GetRequiredService<LoanDbContext>(),
        scope.ServiceProvider.GetRequiredService<ILogger<Program>>());
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Exposed for WebApplicationFactory in integration tests.
public partial class Program { }
