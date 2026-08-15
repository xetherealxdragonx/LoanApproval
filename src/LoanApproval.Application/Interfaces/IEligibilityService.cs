using LoanApproval.Domain.Entities;
using LoanApproval.Domain.Enums;

namespace LoanApproval.Application.Interfaces;

public interface IEligibilityService
{
    /// <summary>
    /// Evaluates an applicant against the current rule set for a requested amount.
    /// Returns the outcome and a human-readable reasoning string suitable for the audit log.
    /// </summary>
    (DecisionType Outcome, string Reasoning) Evaluate(Applicant applicant, decimal requestedAmount);
}
