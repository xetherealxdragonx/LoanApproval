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
public class LoanRepository : ILoanRepository
{
    private readonly LoanDbContext _context;

    public LoanRepository(LoanDbContext context)
    {
        _context = context;
    }

    public async Task<Applicant?> GetApplicantByMemberNumberAsync(string memberNumber)
    {
        return await _context.Applicants
            .FirstOrDefaultAsync(a => a.MemberNumber == memberNumber);
    }

    public async Task<LoanApplication> CreateLoanApplicationAsync(LoanApplication application)
    {
        _context.LoanApplications.Add(application);
        await _context.SaveChangesAsync();
        return application;
    }

    public async Task SaveDecisionAsync(Decision decision)
    {
        _context.Decisions.Add(decision);
        await _context.SaveChangesAsync();
    }

    public async Task MarkFundedAsync(int loanApplicationId, DateTime fundedAtUtc)
    {
        var application = await _context.LoanApplications.FindAsync(loanApplicationId);
        if (application is null) return;

        application.FundedAtUtc = fundedAtUtc;
        await _context.SaveChangesAsync();
    }

    public async Task<LoanApplication?> GetLoanApplicationAsync(int id)
    {
        return await _context.LoanApplications
            .Include(l => l.Decision)
            .Include(l => l.Applicant)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<(int TotalApplications, int Approved, double AvgEvaluationMs)> GetApplicantTrendAsync(int applicantId)
    {
        var query = _context.LoanApplications
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
