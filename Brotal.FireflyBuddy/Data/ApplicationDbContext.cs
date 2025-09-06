using Microsoft.EntityFrameworkCore;
using TickerQ.EntityFrameworkCore.Configurations;

namespace Brotal.FireflyBuddy.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<IngestMessage> IngestMessages { get; set; } = null!;
    public DbSet<AnalysisResult> AnalysisResults { get; set; } = null!;
    public DbSet<TransactionDraft> TransactionDrafts { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Set default schema for all entities
        modelBuilder.HasDefaultSchema("buddy");

        // Configure IngestMessage
        modelBuilder.Entity<IngestMessage>(entity =>
        {
            entity.ToTable("IngestMessages", "buddy");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Text).IsRequired().HasMaxLength(4000);
            entity.Property(e => e.Source).HasMaxLength(100);
            entity.Property(e => e.ExternalId).HasMaxLength(500);
            entity.Property(e => e.ProcessingError).HasMaxLength(1000);

            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ExternalId).IsUnique().HasFilter("\"ExternalId\" IS NOT NULL");

            entity.HasOne(e => e.AnalysisResult)
                  .WithOne(e => e.IngestMessage)
                  .HasForeignKey<AnalysisResult>(e => e.IngestMessageId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.TransactionDraft)
                  .WithOne(e => e.IngestMessage)
                  .HasForeignKey<TransactionDraft>(e => e.IngestMessageId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure AnalysisResult
        modelBuilder.Entity<AnalysisResult>(entity =>
        {
            entity.ToTable("AnalysisResults", "buddy");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reason).HasMaxLength(1000);

            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.IsTransactional);
            entity.HasIndex(e => e.IsConfident);
        });

        // Configure TransactionDraft
        modelBuilder.Entity<TransactionDraft>(entity =>
        {
            entity.ToTable("TransactionDrafts", "buddy");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.CurrencyCode).IsRequired().HasMaxLength(3);
            entity.Property(e => e.SourceAccountName).HasMaxLength(200);
            entity.Property(e => e.DestinationAccountName).HasMaxLength(200);
            entity.Property(e => e.CategoryName).HasMaxLength(200);
            entity.Property(e => e.BudgetName).HasMaxLength(200);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.Tags).HasMaxLength(500);
            entity.Property(e => e.SubscriptionName).HasMaxLength(200);
            entity.Property(e => e.ExternalUrl).HasMaxLength(500);
            entity.Property(e => e.FireflyTransactionId).HasMaxLength(100);
            entity.Property(e => e.SubmissionError).HasMaxLength(1000);

            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");

            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Date);
        });
    }
}
