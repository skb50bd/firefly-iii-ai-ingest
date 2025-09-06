using Brotal.FireflyBuddy.Data;
using System.ComponentModel.DataAnnotations;

namespace Brotal.FireflyBuddy.Models.ViewModels;

public class DashboardViewModel
{
    public List<IngestMessage> RecentMessages { get; set; } = new();
    public List<TransactionDraft> RecentDrafts { get; set; } = new();
}

public class MessagesIndexViewModel
{
    public List<IngestMessage> Messages { get; set; } = new();
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
}

public class CreateMessageViewModel
{
    [Required]
    [MaxLength(4000)]
    public string Text { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Source { get; set; }
}

public class DraftsIndexViewModel
{
    public List<TransactionDraft> Drafts { get; set; } = new();
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
}

public class EditDraftViewModel
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    [Required]
    public DateTimeOffset Date { get; set; }

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
    public string? Tags { get; set; }

    [MaxLength(200)]
    public string? SubscriptionName { get; set; }

    [MaxLength(500)]
    public string? ExternalUrl { get; set; }
}
