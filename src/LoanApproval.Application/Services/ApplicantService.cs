using LoanApproval.Application.DTOs;
using LoanApproval.Application.Interfaces;
using LoanApproval.Domain.Entities;

namespace LoanApproval.Application.Services;

/// <summary>
/// Maps Applicant entities onto the read DTOs the API exposes. The mapping
/// lives here rather than in the controller so the shape of the API response
/// is a decision of the application layer, and the domain entities never leak
/// out through the wire contract.
/// </summary>
public class ApplicantService(ILoanRepository repository) : IApplicantService
{
    public async Task<IReadOnlyList<ApplicantSummaryResponse>> GetApplicantsAsync()
    {
        var applicants = await repository.GetApplicantsAsync();

        return applicants.Select(a => new ApplicantSummaryResponse(
            a.Id,
            a.MemberNumber,
            a.FullName,
            a.MonthlyIncome,
            a.OpenLoanCount,
            a.HasRecentDelinquency,
            a.MemberSince,
            a.LoanApplications.Count)).ToList();
    }

    public async Task<ApplicantDetailResponse?> GetApplicantDetailAsync(string memberNumber)
    {
        var applicant = await repository.GetApplicantDetailAsync(memberNumber);
        if (applicant is null) return null;

        return new ApplicantDetailResponse(
            applicant.Id,
            applicant.MemberNumber,
            applicant.FullName,
            applicant.MonthlyIncome,
            applicant.OpenLoanCount,
            applicant.HasRecentDelinquency,
            applicant.MemberSince,
            applicant.LoanApplications
                // Most recent first: a reviewer looking at a member cares about
                // the latest decision, not the oldest.
                .OrderByDescending(l => l.SubmittedAtUtc)
                .Select(ToSummary)
                .ToList());
    }

    private static LoanApplicationSummaryResponse ToSummary(LoanApplication application) =>
        new(
            application.Id,
            application.RequestedAmount,
            application.SubmittedAtUtc,
            application.FundedAtUtc,
            application.Decision?.Outcome,
            application.Decision?.Reasoning,
            application.Decision?.EvaluatedAtUtc,
            application.Decision?.EvaluationDurationMs);
}
