using Microsoft.Extensions.AI;

namespace Brotal.FireflyBuddy;

public sealed class AiService(IChatClient chatClient)
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

        var response = await chatClient.GetResponseAsync<AiClassificationResult>(
            messages,
            cancellationToken: cancellationToken
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

        return response.Result;
    }

    private static string GetSystemPrompt(FireflyContextSnapshot context)
    {
        var categories      = string.Join(", ", context.Categories.Select(c => c.Attributes.Name));
        var assets          = string.Join(", ", context.AssetAccounts.Select(a => new { a.Attributes.Name, a.Attributes.AccountNumber, a.Attributes.CurrencyCode }));
        var expenses        = string.Join(", ", context.ExpenseAccounts.Select(a => new { a.Attributes.Name, a.Attributes.AccountNumber, a.Attributes.CurrencyCode }));
        var liabilities     = string.Join(", ", context.LiabilityAccounts.Select(a => new { a.Attributes.Name, a.Attributes.AccountNumber, a.Attributes.CurrencyCode, a.Attributes.LiabilityDirection }));
        var revenues        = string.Join(", ", context.RevenueAccounts.Select(a => new { a.Attributes.Name, a.Attributes.AccountNumber, a.Attributes.CurrencyCode }));
        var subscriptions   = string.Join(", ", context.Subscriptions.Select(s => new { s.Attributes.Name, s.Attributes.CurrencyCode, s.Attributes.AmountMin, s.Attributes.AmountMax }));
        var budgets         = string.Join(", ", context.Budgets.Select(b => new { b.Attributes.Name, b.Attributes.CurrencyCode, b.Attributes.AutoBudgetAmount, b.Attributes.AutoBudgetPeriod, b.Attributes.AutoBudgetType }));
        var tags            = string.Join(", ", context.Tags.Select(t => t.Attributes.Tag));

        var prompt = $$$"""
            You are an assistant that extracts financial transactions from arbitrary text (SMS, email, or user notes) for Firefly-III.
            If the text is not a financial transaction, respond with JSON: {{""isTransactional"": false, ""isConfident"": true, ""reason"": ""not a transaction""}}
            If it is a transaction, respond accordingly with best matches. You can set properties to null, where you are not confident about the values.
            Include the original text in the notes field.
            Available context (choose best matches):
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
