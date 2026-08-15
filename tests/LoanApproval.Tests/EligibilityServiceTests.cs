using LoanApproval.Application.Services;
using LoanApproval.Domain.Entities;
using LoanApproval.Domain.Enums;
using Xunit;

namespace LoanApproval.Tests;

public class EligibilityServiceTests
{
    private readonly EligibilityService _sut = new();

    private static Applicant GoodStandingApplicant() => new()
    {
        MonthlyIncome = 3000m,
        OpenLoanCount = 0,
        HasRecentDelinquency = false
    };

    [Fact]
    public void Evaluate_ApprovesWhenAllCriteriaMet()
    {
        var applicant = GoodStandingApplicant();

        var (outcome, reasoning) = _sut.Evaluate(applicant, 300m);

        Assert.Equal(DecisionType.Approved, outcome);
        Assert.Contains("meets all automatic eligibility criteria", reasoning);
    }

    [Fact]
    public void Evaluate_DeniesWhenAmountExceedsCap()
    {
        var applicant = GoodStandingApplicant();

        var (outcome, reasoning) = _sut.Evaluate(applicant, 501m);

        Assert.Equal(DecisionType.Denied, outcome);
        Assert.Contains("exceeds the small-dollar loan cap", reasoning);
    }

    [Fact]
    public void Evaluate_DeniesWhenAmountIsZeroOrNegative()
    {
        var applicant = GoodStandingApplicant();

        var (outcome, _) = _sut.Evaluate(applicant, 0m);

        Assert.Equal(DecisionType.Denied, outcome);
    }

    [Fact]
    public void Evaluate_RoutesToManualReviewWhenRecentDelinquency()
    {
        var applicant = GoodStandingApplicant();
        applicant.HasRecentDelinquency = true;

        var (outcome, reasoning) = _sut.Evaluate(applicant, 200m);

        Assert.Equal(DecisionType.ManualReviewRequired, outcome);
        Assert.Contains("delinquency", reasoning);
    }

    [Fact]
    public void Evaluate_DeniesWhenTooManyOpenLoans()
    {
        var applicant = GoodStandingApplicant();
        applicant.OpenLoanCount = 2;

        var (outcome, reasoning) = _sut.Evaluate(applicant, 200m);

        Assert.Equal(DecisionType.Denied, outcome);
        Assert.Contains("open loans", reasoning);
    }

    [Fact]
    public void Evaluate_RoutesToManualReviewWhenIncomeBelowThreshold()
    {
        var applicant = GoodStandingApplicant();
        applicant.MonthlyIncome = 500m;

        var (outcome, reasoning) = _sut.Evaluate(applicant, 200m);

        Assert.Equal(DecisionType.ManualReviewRequired, outcome);
        Assert.Contains("below the automatic-approval threshold", reasoning);
    }
}
