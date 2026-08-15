namespace LoanApproval.Domain.Entities;

public class LoanApplication
{
    public int Id { get; set; }
    public int ApplicantId { get; set; }
    public Applicant? Applicant { get; set; }

    public decimal RequestedAmount { get; set; }
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? FundedAtUtc { get; set; }

    public Decision? Decision { get; set; }
}
