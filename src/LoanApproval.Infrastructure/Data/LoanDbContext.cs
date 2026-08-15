using LoanApproval.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LoanApproval.Infrastructure.Data;

public class LoanDbContext : DbContext
{
    public LoanDbContext(DbContextOptions<LoanDbContext> options) : base(options) { }

    public DbSet<Applicant> Applicants => Set<Applicant>();
    public DbSet<LoanApplication> LoanApplications => Set<LoanApplication>();
    public DbSet<Decision> Decisions => Set<Decision>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Applicant>(entity =>
        {
            entity.HasIndex(a => a.MemberNumber).IsUnique();
            entity.Property(a => a.MonthlyIncome).HasColumnType("decimal(12,2)");
        });

        modelBuilder.Entity<LoanApplication>(entity =>
        {
            entity.Property(l => l.RequestedAmount).HasColumnType("decimal(12,2)");

            entity.HasOne(l => l.Applicant)
                  .WithMany(a => a.LoanApplications)
                  .HasForeignKey(l => l.ApplicantId);

            entity.HasOne(l => l.Decision)
                  .WithOne(d => d.LoanApplication!)
                  .HasForeignKey<Decision>(d => d.LoanApplicationId);
        });

        base.OnModelCreating(modelBuilder);
    }
}
