using LoanApproval.Domain.Entities;

namespace LoanApproval.Application.Interfaces;

public interface ILoanRepository
{
    Task<Applicant?> GetApplicantByMemberNumberAsync(string memberNumber);
    Task<LoanApplication> CreateLoanApplicationAsync(LoanApplication application);
    Task SaveDecisionAsync(Decision decision);
    Task MarkFundedAsync(int loanApplicationId, DateTime fundedAtUtc);
    Task<LoanApplication?> GetLoanApplicationAsync(int id);

    /// <summary>
    /// Returns approval-rate trend data for an applicant: total applications,
    /// approvals, and the average evaluation time. Used to demonstrate a
    /// non-trivial query (aggregation + join) beyond simple CRUD lookups.
    /// </summary>
    Task<(int TotalApplications, int Approved, double AvgEvaluationMs)> GetApplicantTrendAsync(int applicantId);
}
