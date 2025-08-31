using Brotal.FireflyIII.Model;
using System.Text.Json.Serialization;

namespace Brotal.FireflyBuddy;

public sealed record IngestMessage(string Text);

public sealed class OpenAiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-5-mini";
}

public sealed class OllamaOptions
{
    public string Url { get; set; } = string.Empty;
    public string Model { get; set; } = "gemma3:1b";
}

public sealed class FireflyOptions
{
    public string Url { get; set; } = string.Empty;
    public string PersonalAccessToken { get; set; } = string.Empty;
}

public sealed class AuthenticationOptions
{
    public bool Enabled { get; set; } = false;
    public string[] ApiKeys { get; set; } = [];
    public BasicAuthOptions? BasicAuth { get; set; }
}

public sealed class BasicAuthOptions
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class AiClassificationResult
{
    [JsonPropertyName("isTransactional")]
    public bool IsTransactional { get; set; }

    [JsonPropertyName("isConfident")]
    public bool IsConfident { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("draft")]
    public TransactionDraft? Draft { get; set; }
}

public sealed class TransactionDraft
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [JsonPropertyName("type")]
    public TransactionTypeProperty Type { get; set; } = TransactionTypeProperty.Withdrawal;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public DateTimeOffset Date { get; set; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("currencyCode")]
    public string CurrencyCode { get; set; } = "USD";

    [JsonPropertyName("sourceAccountName")]
    public string SourceAccountName { get; set; } = string.Empty;

    [JsonPropertyName("destinationAccountName")]
    public string DestinationAccountName { get; set; } = string.Empty;

    [JsonPropertyName("categoryName")]
    public string CategoryName { get; set; } = string.Empty;

    [JsonPropertyName("budgetName")]
    public string BudgetName { get; set; } = string.Empty;

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("tags")]
    public string[] Tags { get; set; } = [];

    [JsonPropertyName("subscriptionName")]
    public string? SubscriptionName { get; set; }

    [JsonPropertyName("externalUrl")]
    public string? ExternalUrl { get; set; }
}

public sealed class FireflyContextSnapshot
{
    public required List<AccountRead> AssetAccounts { get; init; }
    public required List<AccountRead> ExpenseAccounts { get; init; }
    public required List<AccountRead> LiabilityAccounts { get; init; }
    public required List<AccountRead> RevenueAccounts { get; init; }
    public required List<CategoryRead> Categories { get; init; }
    public required List<TagRead> Tags { get; init; }
    public required List<BudgetRead> Budgets { get; init; }
    public required List<BillRead> Subscriptions { get; init; }
}
