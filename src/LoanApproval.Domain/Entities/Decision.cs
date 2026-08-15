using LoanApproval.Domain.Enums;

namespace LoanApproval.Domain.Entities;

/// <summary>
/// Immutable audit record of an eligibility decision. Never updated in place -
/// if a decision is reconsidered, a new LoanApplication/Decision pair is created.
/// This mirrors the "Integrity" expectation of a defensible, timestamped trail.
/// </summary>
public class Decision
{
    public int Id { get; set; }
    public int LoanApplicationId { get; set; }
    public LoanApplication? LoanApplication { get; set; }

    public DecisionType Outcome { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public DateTime EvaluatedAtUtc { get; set; } = DateTime.UtcNow;
    public long EvaluationDurationMs { get; set; }
}
