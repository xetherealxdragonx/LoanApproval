using LoanApproval.Application.DTOs;
using LoanApproval.Application.Interfaces;
using LoanApproval.Application.Services;
using LoanApproval.Domain.Entities;
using LoanApproval.Domain.Enums;
using Moq;
using Xunit;

namespace LoanApproval.Tests;

/// <summary>
/// Because LoanApplicationService only depends on interfaces (constructor
/// injection), every collaborator here is mocked - no database, no real
/// funding call, no real logger. This is the payoff of designing around
/// DI from the start: the orchestration logic is testable in isolation.
/// </summary>
public class LoanApplicationServiceTests
{
    private readonly Mock<ILoanRepository> _repository = new();
    private readonly Mock<IEligibilityService> _eligibilityService = new();
    private readonly Mock<IFundingGateway> _fundingGateway = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();

    private LoanApplicationService BuildSut() => new(
        _repository.Object,
        _eligibilityService.Object,
        _fundingGateway.Object,
        _auditLogger.Object);

    [Fact]
    public async Task SubmitAsync_FundsLoanWhenApproved()
    {
        var applicant = new Applicant { Id = 1, MemberNumber = "M100" };
        _repository.Setup(r => r.GetApplicantByMemberNumberAsync("M100"))
            .ReturnsAsync(applicant);
        _repository.Setup(r => r.CreateLoanApplicationAsync(It.IsAny<LoanApplication>()))
            .ReturnsAsync((LoanApplication l) => { l.Id = 42; return l; });
        _eligibilityService.Setup(e => e.Evaluate(applicant, 250m))
            .Returns((DecisionType.Approved, "Meets all criteria."));
        _fundingGateway.Setup(f => f.FundAsync(42, 250m))
            .ReturnsAsync(true);

        var sut = BuildSut();
        var result = await sut.SubmitAsync(new LoanApplicationRequest("M100", 250m));

        Assert.Equal(DecisionType.Approved, result.Outcome);
        Assert.NotNull(result.FundedAtUtc);
        _fundingGateway.Verify(f => f.FundAsync(42, 250m), Times.Once);
        _auditLogger.Verify(a => a.LogDecision(It.IsAny<Decision>(), "M100"), Times.Once);
    }

    [Fact]
    public async Task SubmitAsync_DoesNotCallFundingGatewayWhenDenied()
    {
        var applicant = new Applicant { Id = 2, MemberNumber = "M200" };
        _repository.Setup(r => r.GetApplicantByMemberNumberAsync("M200"))
            .ReturnsAsync(applicant);
        _repository.Setup(r => r.CreateLoanApplicationAsync(It.IsAny<LoanApplication>()))
            .ReturnsAsync((LoanApplication l) => { l.Id = 43; return l; });
        _eligibilityService.Setup(e => e.Evaluate(applicant, 900m))
            .Returns((DecisionType.Denied, "Exceeds cap."));

        var sut = BuildSut();
        var result = await sut.SubmitAsync(new LoanApplicationRequest("M200", 900m));

        Assert.Equal(DecisionType.Denied, result.Outcome);
        Assert.Null(result.FundedAtUtc);
        _fundingGateway.Verify(f => f.FundAsync(It.IsAny<int>(), It.IsAny<decimal>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_ThrowsWhenApplicantNotFound()
    {
        _repository.Setup(r => r.GetApplicantByMemberNumberAsync("UNKNOWN"))
            .ReturnsAsync((Applicant?)null);

        var sut = BuildSut();

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => sut.SubmitAsync(new LoanApplicationRequest("UNKNOWN", 100m)));
    }
}
