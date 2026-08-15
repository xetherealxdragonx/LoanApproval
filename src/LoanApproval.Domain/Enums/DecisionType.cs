namespace LoanApproval.Domain.Enums;

/// <summary>
/// Outcome of an eligibility evaluation.
/// Review exists so borderline cases route to a human instead of forcing
/// a binary approve/deny call on incomplete signal.
/// </summary>
public enum DecisionType
{
    Approved,
    Denied,
    ManualReviewRequired
}
