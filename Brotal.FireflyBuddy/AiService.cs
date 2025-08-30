using Microsoft.Extensions.AI;

namespace Brotal.FireflyBuddy;

public sealed class AiService(IChatClient chatClient, ILogger<AiService> logger)
{
    public async Task<AiClassificationResult> AnalyzeAsync(
        string rawText,
        FireflyContextSnapshot context,
        CancellationToken cancellationToken
    )
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, GetSystemPrompt(context)),
            new(ChatRole.User, rawText)
        };

        logger.LogInformation("Getting Response from AI");
        var response = await chatClient.GetResponseAsync<AiClassificationResult>(
            messages,
            cancellationToken: cancellationToken
        );

        logger.LogInformation(
            "Received AI Response. IsTransactional: {isTransactional}, Reason: {reason}",
            response.Result.IsTransactional,
            response.Result.Reason
        );

        if (response.Result is null)
        {
            return new AiClassificationResult
            {
                IsTransactional = false,
                IsConfident     = false,
                Reason          = "Unparseable model response"
            };
        }

        response.Result.Draft!.Tags = ["AI", .. response.Result.Draft?.Tags];

        return response.Result;
    }

    private static string GetSystemPrompt(FireflyContextSnapshot context)
    {
        var categories      = string.Join(", ", context.Categories.Select(c => new { c.Attributes.Name }));
        var assets          = string.Join(", ", context.AssetAccounts.Select(a => new { a.Attributes.Name, a.Attributes.AccountNumber, a.Attributes.CurrencyCode }));
        var expenses        = string.Join(", ", context.ExpenseAccounts.Select(a => new { a.Attributes.Name, a.Attributes.AccountNumber, a.Attributes.CurrencyCode }));
        var liabilities     = string.Join(", ", context.LiabilityAccounts.Select(a => new { a.Attributes.Name, a.Attributes.AccountNumber, a.Attributes.CurrencyCode, a.Attributes.LiabilityDirection }));
        var revenues        = string.Join(", ", context.RevenueAccounts.Select(a => new { a.Attributes.Name, a.Attributes.AccountNumber, a.Attributes.CurrencyCode }));
        var subscriptions   = string.Join(", ", context.Subscriptions.Select(s => new { s.Attributes.Name, s.Attributes.CurrencyCode, s.Attributes.AmountMin, s.Attributes.AmountMax }));
        var budgets         = string.Join(", ", context.Budgets.Select(b => new { b.Attributes.Name, b.Attributes.CurrencyCode, b.Attributes.AutoBudgetAmount, b.Attributes.AutoBudgetPeriod, b.Attributes.AutoBudgetType }));
        var tags            = string.Join(", ", context.Tags.Select(t => t.Attributes.Tag));

        var prompt = $$$"""
            You are an assistant that extracts financial transactions from arbitrary text (SMS, email, or user notes) for Firefly-III.
            If the text is not a financial transaction, respond with isTransactional: false, isConfident: true, reason: not a transaction
            If it is a transaction, respond accordingly with best matches from the context. 
            Transactions can be of type Withdrawal, Deposit, Transfer
            For Withdrawal, set DestinationAccountName to an appropriate Expense account or keep empty, and SourceAccountName to an appropriate Asset account..
            You can set properties to null, where you are not confident about the values.
            Available context (choose best matches, use exact names):
            ---
            Categories: {{{categories}}}
            Asset accounts: {{{assets}}}
            Expense accounts: {{{expenses}}}
            Revenue accounts: {{{revenues}}}
            Liabilities: {{{subscriptions}}}
            Subscriptions: {{{subscriptions}}}
            Budgets: {{{budgets}}}
            Tags: {{{tags}}}
            ---
            """;

        return prompt;
    }    
}
