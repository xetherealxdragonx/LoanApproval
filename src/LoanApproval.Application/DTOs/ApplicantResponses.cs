using LoanApproval.Domain.Enums;

namespace LoanApproval.Application.DTOs;

/// <summary>
/// One row in the member list. Deliberately does not carry the full application
/// history - only the count - so the list view stays cheap as membership grows.
/// </summary>
public record ApplicantSummaryResponse(
    int Id,
    string MemberNumber,
    string FullName,
    decimal MonthlyIncome,
    int OpenLoanCount,
    bool HasRecentDelinquency,
    DateTime MemberSince,
    int ApplicationCount
);

/// <summary>
/// A single submitted application together with its decision, if one exists.
///
/// The decision fields are nullable because LoanApplicationService writes the
/// application and the decision in two separate SaveChanges calls - an
/// application persisted without a decision is therefore a representable state.
/// The UI surfaces that case as "Pending" rather than pretending it cannot happen.
/// </summary>
public record LoanApplicationSummaryResponse(
    int LoanApplicationId,
    decimal RequestedAmount,
    DateTime SubmittedAtUtc,
    DateTime? FundedAtUtc,
    DecisionType? Outcome,
    string? Reasoning,
    DateTime? EvaluatedAtUtc,
    long? EvaluationDurationMs
);

/// <summary>
/// A member plus their full application history, for the drill-down view.
/// </summary>
public record ApplicantDetailResponse(
    int Id,
    string MemberNumber,
    string FullName,
    decimal MonthlyIncome,
    int OpenLoanCount,
    bool HasRecentDelinquency,
    DateTime MemberSince,
    IReadOnlyList<LoanApplicationSummaryResponse> Applications
);
