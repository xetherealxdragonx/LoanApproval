using LoanApproval.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LoanApproval.Infrastructure.Data;

/// <summary>
/// Demo seed data. Deliberately kept out of <see cref="LoanDbContext.OnModelCreating"/>
/// (i.e. not EF's HasData) so that sample members are never baked into a migration.
///
/// Only Applicants are seeded. LoanApplications and Decisions are intentionally
/// left empty so a demo creates them live through the real request pipeline,
/// which is the part worth showing.
/// </summary>
public static class DbSeeder
{
    private const int DefaultMaxAttempts = 6;

    /// <summary>
    /// Applies pending migrations and seeds demo applicants, retrying with
    /// exponential backoff while the database is unreachable.
    ///
    /// This runs on the startup path in every environment, so a database that is
    /// still waking up - an Azure SQL failover, or a SQL container that has not
    /// finished starting - would otherwise take the whole application down. The
    /// retry absorbs blips of roughly half a minute.
    ///
    /// If every attempt fails the exception is deliberately allowed to escape:
    /// crashing lets App Service (or Docker, or Kubernetes) restart the process,
    /// which is a longer, better supervised retry than anything done in-process.
    /// Starting up regardless would leave a process that answers requests with
    /// errors while still looking healthy to a platform health probe.
    /// </summary>
    public static async Task SeedAsync(
        LoanDbContext context,
        ILogger logger,
        int maxAttempts = DefaultMaxAttempts)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await MigrateAndSeedAsync(context, logger);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts && IsConnectivityFailure(ex))
            {
                // 1s, 2s, 4s, 8s, 16s - about 31s of tolerance over six attempts.
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));

                logger.LogWarning(
                    "Database unavailable on attempt {Attempt} of {MaxAttempts} ({Reason}). Retrying in {DelaySeconds}s.",
                    attempt, maxAttempts, ex.GetBaseException().Message, delay.TotalSeconds);

                await Task.Delay(delay);
            }
        }
    }

    /// <summary>
    /// True when the exception chain indicates the database could not be reached
    /// or timed out. Note that a genuinely broken migration also surfaces as a
    /// SqlException and will therefore be retried before failing - that costs
    /// half a minute on a permanent fault, which is a fair trade for not having
    /// to enumerate transient SQL error numbers.
    /// </summary>
    private static bool IsConnectivityFailure(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException or TimeoutException) return true;
        }

        return false;
    }

    private static async Task MigrateAndSeedAsync(LoanDbContext context, ILogger logger)
    {
        await context.Database.MigrateAsync();

        if (await context.Applicants.AnyAsync())
        {
            logger.LogInformation("Applicants already present; skipping seed.");
            return;
        }

        // Each applicant is chosen to trip exactly one branch of EligibilityService,
        // so a demo can walk the rules engine end to end without editing data.
        var applicants = new[]
        {
            // Passes every rule -> Approved, then funded by MockFundingGateway.
            new Applicant
            {
                MemberNumber = "M-1001",
                FullName = "Dana Whitfield",
                MonthlyIncome = 3_200m,
                OpenLoanCount = 0,
                HasRecentDelinquency = false,
                MemberSince = new DateTime(2019, 3, 14, 0, 0, 0, DateTimeKind.Utc)
            },

            // Delinquency flag -> ManualReviewRequired (checked before open loans
            // and income, so this applicant is otherwise deliberately spotless).
            new Applicant
            {
                MemberNumber = "M-1002",
                FullName = "Marcus Bell",
                MonthlyIncome = 4_100m,
                OpenLoanCount = 0,
                HasRecentDelinquency = true,
                MemberSince = new DateTime(2021, 8, 2, 0, 0, 0, DateTimeKind.Utc)
            },

            // At the open-loan limit of 2 -> Denied.
            new Applicant
            {
                MemberNumber = "M-1003",
                FullName = "Priya Raman",
                MonthlyIncome = 5_600m,
                OpenLoanCount = 2,
                HasRecentDelinquency = false,
                MemberSince = new DateTime(2017, 1, 20, 0, 0, 0, DateTimeKind.Utc)
            },

            // Income below the 800 auto-approval threshold -> ManualReviewRequired.
            new Applicant
            {
                MemberNumber = "M-1004",
                FullName = "Eli Contreras",
                MonthlyIncome = 650m,
                OpenLoanCount = 1,
                HasRecentDelinquency = false,
                MemberSince = new DateTime(2023, 11, 5, 0, 0, 0, DateTimeKind.Utc)
            },

            // Exactly on the income threshold -> Approved. Useful for showing the
            // boundary is inclusive (the rule is "< 800", not "<= 800").
            new Applicant
            {
                MemberNumber = "M-1005",
                FullName = "Ada Nkemelu",
                MonthlyIncome = 800m,
                OpenLoanCount = 1,
                HasRecentDelinquency = false,
                MemberSince = new DateTime(2022, 6, 30, 0, 0, 0, DateTimeKind.Utc)
            }
        };

        context.Applicants.AddRange(applicants);
        await context.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} demo applicants.", applicants.Length);
    }
}
