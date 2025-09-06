using Brotal.FireflyBuddy.Core.Models.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Brotal.FireflyBuddy.Core.Services;

public sealed class AiService(IChatClient chatClient, ILogger<AiService> logger) : IAiService
{
    public async Task<AiClassificationResult> ClassifyMessageAsync(
        string rawText,
        FireflyContextSnapshot context,
        CancellationToken cancellationToken = default)
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

        if (response.Result.Draft is not null)
        {
            response.Result.Draft.Tags = ["AI", .. response.Result.Draft.Tags];
        }

        return response.Result;
    }

    private static string GetSystemPrompt(FireflyContextSnapshot context)
    {
        var categories      = string.Join(", ", context.Categories.Select(c => $"`{c.Attributes.Name}`"));
        var assets          = string.Join(", ", context.AssetAccounts.Select(a => $"`{a.Attributes.Name}`"));
        var expenses        = string.Join(", ", context.ExpenseAccounts.Select(a => $"`{a.Attributes.Name}`"));
        var liabilities     = string.Join(", ", context.LiabilityAccounts.Select(a => $"`{a.Attributes.Name}`"));
        var revenues        = string.Join(", ", context.RevenueAccounts.Select(a => $"`{a.Attributes.Name}`"));
        var subscriptions   = string.Join(", ", context.Subscriptions.Select(s => $"`{s.Attributes.Name}`"));
        var budgets         = string.Join(", ", context.Budgets.Select(b => $"`{b.Attributes.Name}`"));
        var tags            = string.Join(", ", context.Tags.Select(t => $"`{t.Attributes.Tag}`"));

        var prompt = $$$"""
            You are an expert of financial data. You read financial transactions from arbitrary text (SMS, email, or user notes) and structure them for Firefly-III.
            You recognize if a text describes a financial transaction or not.
            Ignore OTPs, fraud alerts, account balance updates, and other non-transactional messages.
            Transactions can be of type Withdrawal, Deposit, Transfer.
            A Withdrawal is money going out of an asset account (expense, bill, payment) to an expense account or a liability account.
            A Deposit is money coming into an asset account from a revenue account.
            A Transfer is money moving between two asset accounts (e.g. checking to savings).
            You must exactly match the account names, category names, budget names, subscription names and tags from the provided context, 
            even though the text might use slightly different names.
            Available context:
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
