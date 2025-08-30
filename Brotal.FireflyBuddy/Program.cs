using Brotal.FireflyBuddy;
using Brotal.FireflyIII.Client;
using Brotal.FireflyIII.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OpenAI;

var cancellationTokenSource = new CancellationTokenSource();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var fireflyOptions = builder.Configuration.GetSection("Firefly").Get<FireflyOptions>()
    ?? throw new InvalidOperationException("Firefly configuration is missing.");

var authOptions = builder.Configuration.GetSection("Authentication").Get<AuthenticationOptions>()
    ?? new AuthenticationOptions();

var aiProvider = builder.Configuration.GetValue<string>("AiProvider");

if (aiProvider?.Equals("OpenAI", StringComparison.OrdinalIgnoreCase) ?? false)
{
    var openAiOptions =
        builder.Configuration.GetSection("OpenAI").Get<OpenAiOptions>()
            ?? throw new InvalidOperationException("OpenAI configuration is missing.");

    if (string.IsNullOrWhiteSpace(openAiOptions.ApiKey))
    {
        throw new InvalidOperationException(
            "OpenAI API key is not configured. Please set the 'OpenAI:ApiKey' configuration value."
        );
    }

    if (string.IsNullOrWhiteSpace(openAiOptions.Model))
    {
        throw new InvalidOperationException(
            "OpenAI model is not configured. Please set the 'OpenAI:Model' configuration value."
        );
    }

    var chatClient =
        new OpenAIClient(openAiOptions.ApiKey)
            .GetChatClient(openAiOptions.Model)
            .AsIChatClient();

    builder.Services
        .AddSingleton(openAiOptions)
        .AddSingleton(chatClient);
}
else if (aiProvider?.Equals("Ollama", StringComparison.OrdinalIgnoreCase) ?? false)
{
    var ollamaOptions =
    builder.Configuration.GetSection("Ollama").Get<OllamaOptions>()
        ?? throw new InvalidOperationException("Ollama configuration is missing.");

    if (string.IsNullOrWhiteSpace(ollamaOptions.ApiKey))
    {
        throw new InvalidOperationException("OpenAI API key is not configured. Please set the 'OpenAI:ApiKey' configuration value.");
    }

    if (string.IsNullOrWhiteSpace(ollamaOptions.Model))
    {
        throw new InvalidOperationException("Ollama model is not configured. Please set the 'Ollama:Model' configuration value.");
    }

    if (string.IsNullOrWhiteSpace(ollamaOptions.Uri))
    {
        throw new InvalidOperationException("Ollama Uri is not configured. Please set the 'Ollama:Uri' configuration value.");
    }

    var chatClient =
        new OllamaApiClient(new Uri(ollamaOptions.Uri), ollamaOptions.Model);

    builder.Services
        .AddSingleton(ollamaOptions)
        .AddSingleton<IChatClient>(chatClient);
}
else
{
    throw new InvalidOperationException($"Unsupported AI provider: {aiProvider}. Supported providers are: OpenAI, Ollama");
}

builder.Services.AddApi(options =>
{
    BearerToken bearerToken = new(fireflyOptions.PersonalAccessToken);
    OAuthToken oauthToken = new(fireflyOptions.PersonalAccessToken);
    options.AddTokens(bearerToken);
    options.AddTokens(oauthToken);
    options.UseProvider<RateLimitProvider<BearerToken>, BearerToken>();
    options.UseProvider<RateLimitProvider<OAuthToken>, OAuthToken>();

    options.AddApiHttpClients(client =>
    {
        client.BaseAddress = new Uri(fireflyOptions.Url);
    }, builder =>
    {
        builder
            .AddRetryPolicy(2)
            .AddTimeoutPolicy(TimeSpan.FromSeconds(30))
            .AddCircuitBreakerPolicy(10, TimeSpan.FromSeconds(30));
    });
});

builder.Services
    .AddSingleton(fireflyOptions)
    .AddSingleton(authOptions)
    .AddSingleton<AiService>()
    .AddSingleton<FireflyClient>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Add authentication middleware
app.UseCustomAuthentication();

// app.UseHttpsRedirection();

app.MapPost("/txn", async (
    [FromBody]      IngestMessage message,
    [FromServices]  AiService ai,
    [FromServices]  FireflyClient client,
    [FromServices]  ILogger<Program> logger,
    CancellationToken ct
) =>
{
    var context = await client.GetContextAsync(ct);
    var analysis = await ai.AnalyzeAsync(message.Text, context, ct);
    if (analysis.IsTransactional && analysis.IsConfident && analysis.Draft is not null)
    {
        var txnId = await client.CreateTransactionAsync(analysis.Draft, message.Text, ct);
        logger.LogInformation("Created transaction {txnId} for message: {message}", txnId, message.Text);
    }
    else
    {
        logger.LogInformation("No transaction created for message: {message}", message.Text);
        if (!analysis.IsTransactional)
        {
            logger.LogInformation(" - Not classified as transactional.");
        }
        else if (!analysis.IsConfident)
        {
            logger.LogInformation(" - Not confident in classification.");
            if (!string.IsNullOrWhiteSpace(analysis.Reason))
            {
                logger.LogInformation(" - Reason: {reason}", analysis.Reason);
            }
        }
        else if (analysis.Draft is null)
        {
            logger.LogInformation(" - No draft provided.");
        }
    }

    return analysis;
})
.WithName("Record Txn");

var fireflyClient = app.Services.GetRequiredService<FireflyClient>();
var ctx = await fireflyClient.GetContextAsync(cancellationTokenSource.Token);
await app.RunAsync(cancellationTokenSource.Token);
