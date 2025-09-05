using Brotal.FireflyBuddy;
using Brotal.FireflyBuddy.Data;
using Brotal.FireflyBuddy.Repositories;
using Brotal.FireflyBuddy.Jobs;
using Brotal.FireflyIII.Client;
using Brotal.FireflyIII.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TickerQ;

var cancellationTokenSource = new CancellationTokenSource();

var builder = WebApplication.CreateBuilder(args);

// Add MVC services
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddOpenApi();

var fireflyOptions = builder.Configuration.GetSection("Firefly").Get<FireflyOptions>()
    ?? throw new InvalidOperationException("Firefly configuration is missing.");

var authOptions = builder.Configuration.GetSection("Authentication").Get<AuthenticationOptions>()
    ?? new AuthenticationOptions();

// Add database connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Database connection string is missing.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add repositories
builder.Services.AddScoped<IIngestMessageRepository, IngestMessageRepository>();
builder.Services.AddScoped<ITransactionDraftRepository, TransactionDraftRepository>();
builder.Services.AddScoped<IAnalysisResultRepository, AnalysisResultRepository>();

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

// Add TickerQ for background job processing
builder.Services.AddTickerQ(options =>
{
    options.SetMaxConcurrency(5);
    options.AddOperationalStore<ApplicationDbContext>(efOpt => 
    {
        efOpt.UseModelCustomizerForMigrations();
    });
    options.AddDashboard(uiopt =>                                                
    {
        uiopt.BasePath = "/tickerq-dashboard"; 
        uiopt.AddDashboardBasicAuth();
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

// Enable static files for CSS/JS
app.UseStaticFiles();

// Enable routing
app.UseRouting();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("Health Check");

// API endpoints
app.MapPost("/api/messages", async (
    [FromBody] IngestMessageRequest request,
    [FromServices] IIngestMessageRepository messageRepository,
    [FromServices] ILogger<Program> logger,
    CancellationToken ct
) =>
{
    var message = new Data.IngestMessage
    {
        Text = request.Text,
        Source = request.Source ?? "API",
        ExternalId = request.ExternalId,
        Status = MessageStatus.Pending
    };
    
    var createdMessage = await messageRepository.CreateAsync(message, ct);
    logger.LogInformation("Created message {messageId} for ingestion", createdMessage.Id);
    
    return Results.Created($"/api/messages/{createdMessage.Id}", new { id = createdMessage.Id });
})
.WithName("Create Message");

app.MapGet("/api/messages", async (
    [FromQuery] int skip = 0,
    [FromQuery] int take = 50,
    [FromServices] IIngestMessageRepository messageRepository,
    CancellationToken ct
) =>
{
    var messages = await messageRepository.GetAllAsync(skip, take, ct);
    return Results.Ok(messages);
})
.WithName("Get Messages");

app.MapGet("/api/messages/{id:guid}", async (
    Guid id,
    [FromServices] IIngestMessageRepository messageRepository,
    CancellationToken ct
) =>
{
    var message = await messageRepository.GetByIdAsync(id, ct);
    return message is not null ? Results.Ok(message) : Results.NotFound();
})
.WithName("Get Message");

app.MapGet("/api/drafts", async (
    [FromQuery] int skip = 0,
    [FromQuery] int take = 50,
    [FromServices] ITransactionDraftRepository draftRepository,
    CancellationToken ct
) =>
{
    var drafts = await draftRepository.GetAllAsync(skip, take, ct);
    return Results.Ok(drafts);
})
.WithName("Get Drafts");

app.MapPut("/api/drafts/{id:guid}", async (
    Guid id,
    [FromBody] UpdateDraftRequest request,
    [FromServices] ITransactionDraftRepository draftRepository,
    CancellationToken ct
) =>
{
    var draft = await draftRepository.GetByIdAsync(id, ct);
    if (draft is null)
        return Results.NotFound();
    
    // Update draft properties
    draft.Description = request.Description ?? draft.Description;
    draft.Amount = request.Amount ?? draft.Amount;
    draft.Date = request.Date ?? draft.Date;
    draft.SourceAccountName = request.SourceAccountName ?? draft.SourceAccountName;
    draft.DestinationAccountName = request.DestinationAccountName ?? draft.DestinationAccountName;
    draft.CategoryName = request.CategoryName ?? draft.CategoryName;
    draft.BudgetName = request.BudgetName ?? draft.BudgetName;
    draft.Notes = request.Notes ?? draft.Notes;
    draft.Tags = request.Tags ?? draft.Tags;
    draft.Status = DraftStatus.Ready; // Mark as ready for submission
    
    var updatedDraft = await draftRepository.UpdateAsync(draft, ct);
    return Results.Ok(updatedDraft);
})
.WithName("Update Draft");

app.MapPost("/api/drafts/{id:guid}/submit", async (
    Guid id,
    [FromServices] ITransactionDraftRepository draftRepository,
    [FromServices] FireflyClient fireflyClient,
    [FromServices] ILogger<Program> logger,
    CancellationToken ct
) =>
{
    var draft = await draftRepository.GetByIdAsync(id, ct);
    if (draft is null)
        return Results.NotFound();
    
    try
    {
        // Convert to Firefly format
        var fireflyDraft = new TransactionDraft
        {
            Type = draft.Type,
            Description = draft.Description,
            Date = draft.Date,
            Amount = draft.Amount,
            CurrencyCode = draft.CurrencyCode,
            SourceAccountName = draft.SourceAccountName,
            DestinationAccountName = draft.DestinationAccountName,
            CategoryName = draft.CategoryName,
            BudgetName = draft.BudgetName,
            Notes = draft.Notes,
            Tags = draft.Tags?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [],
            SubscriptionName = draft.SubscriptionName,
            ExternalUrl = draft.ExternalUrl
        };
        
        var transactionId = await fireflyClient.CreateTransactionAsync(fireflyDraft, draft.IngestMessage.Text, ct);
        
        draft.Status = DraftStatus.Submitted;
        draft.SubmittedAt = DateTime.UtcNow;
        draft.FireflyTransactionId = transactionId;
        await draftRepository.UpdateAsync(draft, ct);
        
        logger.LogInformation("Submitted draft {draftId} as transaction {transactionId}", draft.Id, transactionId);
        return Results.Ok(new { transactionId });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error submitting draft {draftId}", draft.Id);
        draft.Status = DraftStatus.Failed;
        draft.SubmissionError = ex.Message;
        await draftRepository.UpdateAsync(draft, ct);
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("Submit Draft");

// Legacy endpoint for backward compatibility
app.MapPost("/txn", async (
    [FromBody] IngestMessage message,
    [FromServices] IIngestMessageRepository messageRepository,
    [FromServices] ILogger<Program> logger,
    CancellationToken ct
) =>
{
    var dbMessage = new Data.IngestMessage
    {
        Text = message.Text,
        Source = "Legacy API",
        Status = MessageStatus.Pending
    };
    
    var createdMessage = await messageRepository.CreateAsync(dbMessage, ct);
    logger.LogInformation("Created message {messageId} via legacy API", createdMessage.Id);
    
    return Results.Ok(new { messageId = createdMessage.Id, status = "queued" });
})
.WithName("Legacy Record Txn");

// Map MVC controllers
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Initialize TickerQ
app.UseTickerQ();

var fireflyClient = app.Services.GetRequiredService<FireflyClient>();
var ctx = await fireflyClient.GetContextAsync(cancellationTokenSource.Token);
await app.RunAsync(cancellationTokenSource.Token);

// Request/Response models
public record IngestMessageRequest(string Text, string? Source = null, string? ExternalId = null);
public record UpdateDraftRequest(
    string? Description,
    decimal? Amount,
    DateTimeOffset? Date,
    string? SourceAccountName,
    string? DestinationAccountName,
    string? CategoryName,
    string? BudgetName,
    string? Notes,
    string? Tags
);
