namespace Brotal.FireflyBuddy.Core.Models.Responses;

public sealed record MessageCreatedResponse(Guid Id);

public sealed record TransactionSubmittedResponse(string TransactionId);

public sealed record HealthCheckResponse(string Status, DateTime Timestamp);
