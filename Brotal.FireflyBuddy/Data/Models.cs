using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Brotal.FireflyIII.Model;

namespace Brotal.FireflyBuddy.Data;

public class IngestMessage
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(4000)]
    public string Text { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Source { get; set; } // e.g., "SMS", "Email", "Manual"

    [MaxLength(500)]
    public string? ExternalId { get; set; } // External system ID if applicable

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }

    public MessageStatus Status { get; set; } = MessageStatus.Pending;

    [MaxLength(1000)]
    public string? ProcessingError { get; set; }

    // Navigation properties
    public virtual AnalysisResult? AnalysisResult { get; set; }
    public virtual TransactionDraft? TransactionDraft { get; set; }
}

public class AnalysisResult
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid IngestMessageId { get; set; }

    public bool IsTransactional { get; set; }

    public bool IsConfident { get; set; }

    [MaxLength(1000)]
    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(IngestMessageId))]
    public virtual IngestMessage IngestMessage { get; set; } = null!;
}

public class TransactionDraft
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid IngestMessageId { get; set; }

    public TransactionTypeProperty Type { get; set; } = TransactionTypeProperty.Withdrawal;

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public DateTimeOffset Date { get; set; } = DateTimeOffset.UtcNow;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(3)]
    public string CurrencyCode { get; set; } = "USD";

    [MaxLength(200)]
    public string SourceAccountName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string DestinationAccountName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string CategoryName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string BudgetName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [MaxLength(500)]
    public string? Tags { get; set; } // JSON array as string

    [MaxLength(200)]
    public string? SubscriptionName { get; set; }

    [MaxLength(500)]
    public string? ExternalUrl { get; set; }

    public DraftStatus Status { get; set; } = DraftStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? SubmittedAt { get; set; }

    [MaxLength(100)]
    public string? FireflyTransactionId { get; set; }

    [MaxLength(1000)]
    public string? SubmissionError { get; set; }

    // Navigation properties
    [ForeignKey(nameof(IngestMessageId))]
    public virtual IngestMessage IngestMessage { get; set; } = null!;
}

public enum MessageStatus
{
    Pending,
    Processing,
    Processed,
    Failed,
    Skipped
}

public enum DraftStatus
{
    Pending,
    Ready,
    Submitted,
    Failed,
    Cancelled
}
