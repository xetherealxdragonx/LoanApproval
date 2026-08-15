using LoanApproval.Application.DTOs;

namespace LoanApproval.Application.Interfaces;

public interface ILoanApplicationService
{
    Task<LoanDecisionResponse> SubmitAsync(LoanApplicationRequest request);
}
