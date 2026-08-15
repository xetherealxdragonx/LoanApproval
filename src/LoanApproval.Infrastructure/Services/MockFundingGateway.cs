using LoanApproval.Application.Interfaces;

namespace LoanApproval.Infrastructure.Services;

/// <summary>
/// Stand-in for a real ACH/payments integration. Swappable via DI - the
/// Application layer only ever depends on IFundingGateway, so replacing
/// this with a real processor client later requires zero changes upstream.
/// </summary>
public class MockFundingGateway : IFundingGateway
{
    public async Task<bool> FundAsync(int loanApplicationId, decimal amount)
    {
        // Simulate network latency for the downstream funding call.
        await Task.Delay(150);
        return true;
    }
}
