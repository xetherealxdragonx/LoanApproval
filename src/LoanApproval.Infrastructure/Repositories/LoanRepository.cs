using LoanApproval.Application.Interfaces;
using LoanApproval.Domain.Entities;
using LoanApproval.Domain.Enums;
using LoanApproval.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LoanApproval.Infrastructure.Repositories;

/// <summary>
/// Registered as Scoped in DI - it wraps a DbContext, which itself is
/// Scoped, so their lifetimes must match. Using Singleton here would leak
/// a DbContext across requests; using Transient would create redundant
/// contexts within the same request. This is a good live example to walk
/// through if asked to explain DI lifetimes.
/// </summary>
public class LoanRepository(LoanDbContext context) : ILoanRepository
{
    public async Task<Applicant?> GetApplicantByMemberNumberAsync(string memberNumber)
    {
        return await context.Applicants
            .FirstOrDefaultAsync(a => a.MemberNumber == memberNumber);
    }

    public async Task<LoanApplication> CreateLoanApplicationAsync(LoanApplication application)
    {
        context.LoanApplications.Add(application);
        await context.SaveChangesAsync();
        return application;
    }

    public async Task SaveDecisionAsync(Decision decision)
    {
        context.Decisions.Add(decision);
        await context.SaveChangesAsync();
    }

    public async Task MarkFundedAsync(int loanApplicationId, DateTime fundedAtUtc)
    {
        var application = await context.LoanApplications.FindAsync(loanApplicationId);
        if (application is null) return;

        application.FundedAtUtc = fundedAtUtc;
        await context.SaveChangesAsync();
    }

    public async Task<LoanApplication?> GetLoanApplicationAsync(int id)
    {
        return await context.LoanApplications
            .Include(l => l.Decision)
            .Include(l => l.Applicant)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<(int TotalApplications, int Approved, double AvgEvaluationMs)> GetApplicantTrendAsync(int applicantId)
    {
        var query = context.LoanApplications
            .Where(l => l.ApplicantId == applicantId)
            .Include(l => l.Decision)
            .Where(l => l.Decision != null);

        var total = await query.CountAsync();
        var approved = await query.CountAsync(l => l.Decision!.Outcome == DecisionType.Approved);
        var avgMs = total == 0
            ? 0
            : await query.AverageAsync(l => l.Decision!.EvaluationDurationMs);

        return (total, approved, avgMs);
    }
}
