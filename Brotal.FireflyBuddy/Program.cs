using Brotal.FireflyBuddy.Data;
using Brotal.FireflyBuddy.Jobs;
using Brotal.FireflyBuddy.Middleware;
using Brotal.FireflyBuddy.Models.Configuration;
using Brotal.FireflyBuddy.Repositories;
using Brotal.FireflyBuddy.Services;
using Brotal.FireflyIII.Client;
using Brotal.FireflyIII.Extensions;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using TickerQ.Dashboard.DependencyInjection;
using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore.DependencyInjection;

var cancellationTokenSource = new CancellationTokenSource();

var builder = WebApplication.CreateBuilder(args);

// Add MVC services
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Add API controllers
builder.Services.AddControllers();

// Add OpenAPI generation for Scalar
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

// Add services
builder.Services.AddScoped<IAiService, AiService>();
builder.Services.AddScoped<IFireflyClient, FireflyClient>();

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
        efOpt.CancelMissedTickersOnAppStart();
    });
    options.AddDashboard(uiopt =>
    {
        uiopt.BasePath              = "/jobs";
        uiopt.EnableBuiltInAuth     = true;
        uiopt.UseHostAuthentication = false;
        uiopt.RequiredPolicies      = ["Admin", "JobsDashboardAccess"];
    });
});

builder.Services
    .AddSingleton(fireflyOptions)
    .AddSingleton(authOptions)
    .AddScoped<MessageProcessingJobs>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Generate OpenAPI document
    app.MapOpenApi();

    // Add Scalar API documentation
    app.MapScalarApiReference();
}

// Add authentication middleware
app.UseCustomAuthentication();

// app.UseHttpsRedirection();

// Enable static files for CSS/JS
app.UseStaticFiles();

// Enable routing
app.UseRouting();

app.UseAuthorization();

// Map API controllers
app.MapControllers();

// Map MVC controllers
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Initialize TickerQ
app.UseTickerQ();

// var fireflyClient = app.Services.GetRequiredService<IFireflyClient>();
// var ctx = await fireflyClient.GetContextAsync(cancellationTokenSource.Token);
await app.RunAsync(cancellationTokenSource.Token);
