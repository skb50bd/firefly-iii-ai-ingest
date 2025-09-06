using Brotal.FireflyBuddy.Models.Configuration;
using Brotal.FireflyBuddy.Models.AI;

namespace Brotal.FireflyBuddy.Services;

public interface IAiService
{
    Task<AiClassificationResult> ClassifyMessageAsync(string message, FireflyContextSnapshot context, CancellationToken cancellationToken = default);
}

public interface IFireflyClient
{
    Task<string> CreateTransactionAsync(FireflyTransactionDraft draft, string originalMessage, CancellationToken cancellationToken = default);
    Task<FireflyContextSnapshot> GetContextAsync(CancellationToken cancellationToken = default);
}
