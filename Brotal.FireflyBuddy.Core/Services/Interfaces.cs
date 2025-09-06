using Brotal.FireflyBuddy.Core.Models.Configuration;
using Brotal.FireflyBuddy.Core.Models.AI;

namespace Brotal.FireflyBuddy.Core.Services;

public interface IAiService
{
    Task<AiClassificationResult> ClassifyMessageAsync(string message, FireflyContextSnapshot context, CancellationToken cancellationToken = default);
}

public interface IFireflyClient
{
    Task<string> CreateTransactionAsync(FireflyTransactionDraft draft, string originalMessage, CancellationToken cancellationToken = default);
    Task<FireflyContextSnapshot> GetContextAsync(CancellationToken cancellationToken = default);
}
