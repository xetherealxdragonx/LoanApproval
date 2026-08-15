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
    /// All members, each with their loan applications loaded so a caller can
    /// report application counts without an N+1 query. Decisions are not
    /// loaded - see <see cref="GetApplicantDetailAsync"/> for that.
    /// </summary>
    Task<IReadOnlyList<Applicant>> GetApplicantsAsync();

    /// <summary>
    /// A single member with their applications and each application's decision,
    /// for the drill-down view. Returns null if the member number is unknown.
    /// </summary>
    Task<Applicant?> GetApplicantDetailAsync(string memberNumber);

    /// <summary>
    /// Returns approval-rate trend data for an applicant: total applications,
    /// approvals, and the average evaluation time. Used to demonstrate a
    /// non-trivial query (aggregation + join) beyond simple CRUD lookups.
    /// </summary>
    Task<(int TotalApplications, int Approved, double AvgEvaluationMs)> GetApplicantTrendAsync(int applicantId);
}
