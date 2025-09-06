using Brotal.FireflyBuddy.Core.Data;
using Microsoft.EntityFrameworkCore;
using TickerQ.Dashboard.DependencyInjection;
using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore.DependencyInjection;

var cts = new CancellationTokenSource();

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorPages();

// Add database connection
var connectionString = builder.Configuration.GetConnectionString("BuddyDb")
    ?? throw new InvalidOperationException("Database connection string is missing.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add TickerQ for background job processing
builder.Services.AddTickerQ(options =>
{
    options.SetMaxConcurrency(maxConcurrency: 5);

    options.UpdateMissedJobCheckDelay(TimeSpan.FromMinutes(60));

    options.AddOperationalStore<ApplicationDbContext>(efOpt =>
    {
        efOpt.UseModelCustomizerForMigrations();

        // On app start, cancel tickers left in Expired or InProgress (terminated) states
        // to prevent duplicate re-execution after crashes or abrupt shutdowns.
        efOpt.CancelMissedTickersOnAppStart();
        efOpt.IgnoreSeedMemoryCronTickers();
    });

    options.AddDashboard(dbopt =>
    {
        dbopt.BasePath               = "/";
        dbopt.EnableBuiltInAuth      = true;
        dbopt.UseHostAuthentication  = false;

        // Backend API domain (scheme/SSL prefix supported).
        //dbopt.BackendDomain = builder.Configuration.GetValue<string>("BackendDomain");
        //dbopt.RequiredRoles          = ["Admin", "Ops"];
        //dbopt.RequiredPolicies       = ["JobsDashboardAccess"];
        dbopt.EnableBasicAuth        = true;

        dbopt.PreDashboardMiddleware = app => { /* e.g., request logging */ };
        dbopt.CustomMiddleware      = app => { /* e.g., extra auth/rate limits */ };
        dbopt.PostDashboardMiddleware = app => { /* e.g., metrics collection */ };
    });
});


var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

var dbContext = 
    app.Services.CreateScope()
        .ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

if (dbContext.Database.GetPendingMigrations().Any())
{
    await dbContext.Database.MigrateAsync(cts.Token);
}

app.UseTickerQ();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

await app.RunAsync();
