namespace LoanApproval.Domain.Entities;

public class LoanApplication
{
    public int Id { get; set; }
    public int ApplicantId { get; init; }
    public Applicant? Applicant { get; init; }

    public decimal RequestedAmount { get; init; }
    public DateTime SubmittedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime? FundedAtUtc { get; set; }

    public Decision? Decision { get; init; }
}
