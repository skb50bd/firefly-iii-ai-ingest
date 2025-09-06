using System.ComponentModel.DataAnnotations;

namespace Brotal.FireflyBuddy.Core.Models.Requests;

public sealed record UpdateDraftRequest(
    [MaxLength(500)] string? Description,
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")] decimal? Amount,
    DateTimeOffset? Date,
    [MaxLength(200)] string? SourceAccountName,
    [MaxLength(200)] string? DestinationAccountName,
    [MaxLength(200)] string? CategoryName,
    [MaxLength(200)] string? BudgetName,
    [MaxLength(1000)] string? Notes,
    [MaxLength(500)] string? Tags
);
