namespace LoanApproval.Application.Interfaces;

/// <summary>
/// Abstracts the actual funds-transfer call. In production this would call
/// a real payments processor; here it's mocked to simulate Q-Cash's
/// "funding within 60 seconds" behavior without a real integration.
/// </summary>
public interface IFundingGateway
{
    Task<bool> FundAsync(int loanApplicationId, decimal amount);
}
