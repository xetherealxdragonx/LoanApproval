using LoanApproval.Domain.Entities;

namespace LoanApproval.Application.Interfaces;

public interface IAuditLogger
{
    void LogDecision(Decision decision, string memberNumber);
}
