using Brotal.FireflyBuddy.Core.Data;
using Brotal.FireflyBuddy.Core.Jobs;
using Brotal.FireflyBuddy.Core.Models.Configuration;
using Brotal.FireflyBuddy.Core.Repositories;
using Brotal.FireflyBuddy.Core.Services;
using Brotal.FireflyBuddy.Web.Components;
using Brotal.FireflyBuddy.Web.Middleware;
using Brotal.FireflyIII.Client;
using Brotal.FireflyIII.Extensions;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var cancellationTokenSource = new CancellationTokenSource();

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add OpenAPI generation for Scalar
builder.Services.AddOpenApi();

var fireflyOptions = builder.Configuration.GetSection("Firefly").Get<FireflyOptions>()
    ?? throw new InvalidOperationException("Firefly configuration is missing.");

var authOptions = builder.Configuration.GetSection("Authentication").Get<AuthenticationOptions>()
    ?? new AuthenticationOptions();

// Add database connection
var connectionString = builder.Configuration.GetConnectionString("BuddyDb")
    ?? throw new InvalidOperationException("Database connection string is missing.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add repositories
builder.Services.AddScoped<IIngestMessageRepository, IngestMessageRepository>();
builder.Services.AddScoped<ITransactionDraftRepository, TransactionDraftRepository>();
builder.Services.AddScoped<IAnalysisResultRepository, AnalysisResultRepository>();

// Add services
builder.Services.AddScoped<IAiService, AiService>();
builder.Services.AddScoped<IFireflyClient, FireflyClient>();

builder.Services.AddAiChatClient(builder.Configuration);

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
    .AddScoped<MessageProcessingJobs>();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    // Generate OpenAPI document
    app.MapOpenApi();

    // Add Scalar API documentation
    app.MapScalarApiReference();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHttpsRedirection();

    // app.UseHsts();
}

// Add authentication middleware
// app.UseCustomAuthentication();

app.UseAntiforgery();

// Map API controllers
app.MapControllers();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// var fireflyClient = app.Services.GetRequiredService<IFireflyClient>();
// var ctx = await fireflyClient.GetContextAsync(cancellationTokenSource.Token);
await app.RunAsync(cancellationTokenSource.Token);
