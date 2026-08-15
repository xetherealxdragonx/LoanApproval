using LoanApproval.Domain.Enums;

namespace LoanApproval.Domain.Entities;

/// <summary>
/// Immutable audit record of an eligibility decision. Never updated in place -
/// if a decision is reconsidered, a new LoanApplication/Decision pair is created.
/// This mirrors the "Integrity" expectation of a defensible, timestamped trail.
/// </summary>
public class Decision
{
    public int Id { get; init; }
    public int LoanApplicationId { get; init; }
    public LoanApplication? LoanApplication { get; init; }

    public DecisionType Outcome { get; init; }
    public string Reasoning { get; init; } = string.Empty;
    public DateTime EvaluatedAtUtc { get; init; } = DateTime.UtcNow;
    public long EvaluationDurationMs { get; init; }
}
