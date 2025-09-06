using System.ComponentModel.DataAnnotations;

namespace Brotal.FireflyBuddy.Models.Requests;

public sealed record IngestMessageRequest(
    [Required] [MaxLength(4000)] string Text,
    [MaxLength(100)] string? Source = null,
    [MaxLength(200)] string? ExternalId = null
);
