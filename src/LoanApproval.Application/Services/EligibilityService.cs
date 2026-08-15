using LoanApproval.Application.Interfaces;
using LoanApproval.Domain.Entities;
using LoanApproval.Domain.Enums;

namespace LoanApproval.Application.Services;

/// <summary>
/// Simple, explainable rules engine. Deliberately not a black box -
/// every rejection or review outcome carries a plain-English reason,
/// which matters both for member communication and compliance audit.
///
/// Registered as a Singleton in DI: the rule thresholds below are stateless
/// and read-only per evaluation, so there's no need to pay for a new
/// instance per request. Contrast with ILoanRepository, which is Scoped
/// because it wraps a per-request DbContext.
/// </summary>
public class EligibilityService : IEligibilityService
{
    private const decimal MaxLoanAmount = 500m;
    private const decimal MinMonthlyIncome = 800m;
    private const int MaxOpenLoans = 2;

    public (DecisionType Outcome, string Reasoning) Evaluate(Applicant applicant, decimal requestedAmount)
    {
        if (requestedAmount <= 0)
        {
            return (DecisionType.Denied, "Requested amount must be greater than zero.");
        }

        if (requestedAmount > MaxLoanAmount)
        {
            return (DecisionType.Denied,
                $"Requested amount ${requestedAmount:N2} exceeds the small-dollar loan cap of ${MaxLoanAmount:N2}.");
        }

        if (applicant.HasRecentDelinquency)
        {
            return (DecisionType.ManualReviewRequired,
                "Applicant has a recent delinquency flag; routing to manual review rather than an automatic denial.");
        }

        if (applicant.OpenLoanCount >= MaxOpenLoans)
        {
            return (DecisionType.Denied,
                $"Applicant already has {applicant.OpenLoanCount} open loans, at or above the limit of {MaxOpenLoans}.");
        }

        if (applicant.MonthlyIncome < MinMonthlyIncome)
        {
            return (DecisionType.ManualReviewRequired,
                $"Monthly income ${applicant.MonthlyIncome:N2} is below the automatic-approval threshold of ${MinMonthlyIncome:N2}; routing to manual review.");
        }

        return (DecisionType.Approved,
            $"Applicant meets all automatic eligibility criteria for ${requestedAmount:N2}.");
    }
}
