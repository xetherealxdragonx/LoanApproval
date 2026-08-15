namespace LoanApproval.Domain.Entities;

public class Applicant
{
    public int Id { get; set; }
    public string MemberNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public decimal MonthlyIncome { get; set; }
    public int OpenLoanCount { get; set; }
    public bool HasRecentDelinquency { get; set; }
    public DateTime MemberSince { get; set; }

    public ICollection<LoanApplication> LoanApplications { get; set; } = new List<LoanApplication>();
}
