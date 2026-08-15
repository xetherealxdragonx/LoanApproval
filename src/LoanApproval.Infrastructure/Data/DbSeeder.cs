using LoanApproval.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LoanApproval.Infrastructure.Data;

/// <summary>
/// Demo seed data. Deliberately kept out of <see cref="LoanDbContext.OnModelCreating"/>
/// (i.e. not EF's HasData) so that sample members are never baked into a migration
/// and shipped to a real environment - a migration runs everywhere, including
/// production, whereas this seeder is invoked only from the Development branch
/// of the API's startup path.
///
/// Only Applicants are seeded. LoanApplications and Decisions are intentionally
/// left empty so a demo creates them live through the real request pipeline,
/// which is the part worth showing.
/// </summary>
public static class DbSeeder
{
    /// <summary>
    /// Applies any pending migrations, then inserts the demo applicants if the
    /// table is empty. Idempotent: safe to call on every startup.
    /// </summary>
    public static async Task SeedAsync(LoanDbContext context, ILogger logger)
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
