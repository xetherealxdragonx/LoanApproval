using LoanApproval.Application.Interfaces;
using LoanApproval.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace LoanApproval.Infrastructure.Services;

public class AuditLogger(ILogger<AuditLogger> logger) : IAuditLogger
{
    public void LogDecision(Decision decision, string memberNumber)
    {
        logger.LogInformation(
            "Decision {Outcome} for member {MemberNumber} on application {LoanApplicationId} at {EvaluatedAtUtc}. Reasoning: {Reasoning}",
            decision.Outcome,
            memberNumber,
            decision.LoanApplicationId,
            decision.EvaluatedAtUtc,
            decision.Reasoning);
    }
}
