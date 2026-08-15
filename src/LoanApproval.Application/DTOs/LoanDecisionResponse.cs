using LoanApproval.Domain.Enums;

namespace LoanApproval.Application.DTOs;

public record LoanDecisionResponse(
    int LoanApplicationId,
    DecisionType Outcome,
    string Reasoning,
    long EvaluationDurationMs,
    DateTime? FundedAtUtc
);
