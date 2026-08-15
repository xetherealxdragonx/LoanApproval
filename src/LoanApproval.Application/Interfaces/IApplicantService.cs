using LoanApproval.Application.DTOs;

namespace LoanApproval.Application.Interfaces;

/// <summary>
/// Read-side queries backing the member browser UI. Kept separate from
/// <see cref="ILoanApplicationService"/> because that interface owns the
/// write path (submit -> evaluate -> fund) and has very different
/// dependencies; a read model has no need for the eligibility engine,
/// the funding gateway, or the audit logger.
/// </summary>
public interface IApplicantService
{
    Task<IReadOnlyList<ApplicantSummaryResponse>> GetApplicantsAsync();

    /// <summary>
    /// Returns the member and their application history, or null if the
    /// member number is unknown. Null rather than an exception, because the
    /// caller here is a GET that maps a miss straight to 404.
    /// </summary>
    Task<ApplicantDetailResponse?> GetApplicantDetailAsync(string memberNumber);
}
