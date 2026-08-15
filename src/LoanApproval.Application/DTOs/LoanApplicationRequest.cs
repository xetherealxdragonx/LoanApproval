namespace LoanApproval.Application.DTOs;

public record LoanApplicationRequest(
    string MemberNumber,
    decimal RequestedAmount
);
