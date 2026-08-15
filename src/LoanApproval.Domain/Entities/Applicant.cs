namespace LoanApproval.Domain.Entities;

public class Applicant
{
    public int Id { get; init; }
    public string MemberNumber { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public decimal MonthlyIncome { get; set; }
    public int OpenLoanCount { get; set; }
    public bool HasRecentDelinquency { get; set; }
    public DateTime MemberSince { get; init; }

    public ICollection<LoanApplication> LoanApplications { get; init; } = new List<LoanApplication>();
}
