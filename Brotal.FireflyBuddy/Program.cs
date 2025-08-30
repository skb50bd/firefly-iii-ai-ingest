using Brotal.FireflyBuddy;
using Brotal.FireflyIII.Client;
using Brotal.FireflyIII.Extensions;
using Microsoft.AspNetCore.Mvc;

var cancellationTokenSource = new CancellationTokenSource();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var fireflyOptions = builder.Configuration.GetSection("Firefly").Get<FireflyOptions>()
    ?? throw new InvalidOperationException("Firefly configuration is missing.");

var authOptions = builder.Configuration.GetSection("Authentication").Get<AuthenticationOptions>()
    ?? new AuthenticationOptions();

builder.AddAiChatClient();

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
