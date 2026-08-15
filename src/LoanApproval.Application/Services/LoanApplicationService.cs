using System.Diagnostics;
using LoanApproval.Application.DTOs;
using LoanApproval.Application.Interfaces;
using LoanApproval.Domain.Entities;
using LoanApproval.Domain.Enums;

namespace LoanApproval.Application.Services;

/// <summary>
/// Orchestrates a single loan application end to end: load the applicant,
/// run the eligibility engine, persist the decision, and - if approved -
/// call the funding gateway. Every dependency arrives via the constructor,
/// none is newed-up here, which is what makes this class trivial to unit
/// test with mocks for all four collaborators.
/// </summary>
public class LoanApplicationService : ILoanApplicationService
{
    private readonly ILoanRepository _repository;
    private readonly IEligibilityService _eligibilityService;
    private readonly IFundingGateway _fundingGateway;
    private readonly IAuditLogger _auditLogger;

    public LoanApplicationService(
        ILoanRepository repository,
        IEligibilityService eligibilityService,
        IFundingGateway fundingGateway,
        IAuditLogger auditLogger)
    {
        _repository = repository;
        _eligibilityService = eligibilityService;
        _fundingGateway = fundingGateway;
        _auditLogger = auditLogger;
    }

    public async Task<LoanDecisionResponse> SubmitAsync(LoanApplicationRequest request)
    {
        var applicant = await _repository.GetApplicantByMemberNumberAsync(request.MemberNumber)
            ?? throw new KeyNotFoundException($"No applicant found for member number '{request.MemberNumber}'.");

        var loanApplication = await _repository.CreateLoanApplicationAsync(new LoanApplication
        {
            ApplicantId = applicant.Id,
            RequestedAmount = request.RequestedAmount
        });

        var stopwatch = Stopwatch.StartNew();
        var (outcome, reasoning) = _eligibilityService.Evaluate(applicant, request.RequestedAmount);
        stopwatch.Stop();

        var decision = new Decision
        {
            LoanApplicationId = loanApplication.Id,
            Outcome = outcome,
            Reasoning = reasoning,
            EvaluationDurationMs = stopwatch.ElapsedMilliseconds
        };

        await _repository.SaveDecisionAsync(decision);
        _auditLogger.LogDecision(decision, request.MemberNumber);

        DateTime? fundedAt = null;
        if (outcome == DecisionType.Approved)
        {
            var funded = await _fundingGateway.FundAsync(loanApplication.Id, request.RequestedAmount);
            if (funded)
            {
                fundedAt = DateTime.UtcNow;
                await _repository.MarkFundedAsync(loanApplication.Id, fundedAt.Value);
            }
        }

        return new LoanDecisionResponse(
            loanApplication.Id,
            outcome,
            reasoning,
            decision.EvaluationDurationMs,
            fundedAt);
    }
}
