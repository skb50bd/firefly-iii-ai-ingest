var cts = new CancellationTokenSource();

var builder = DistributedApplication.CreateBuilder(args);

var username = builder.AddParameter("pg-user", secret: true);
var password = builder.AddParameter("pg-password", secret: true);

var postgres = 
    builder
        .AddPostgres("postgres", username, password)
        .WithDataVolume("BuddyDb")
        .WithLifetime(ContainerLifetime.Persistent)
        .WithPgAdmin();

var db = postgres.AddDatabase("BuddyDb");

var ollama = 
    builder
        .AddOllama("Ollama")
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDataVolume("buddy-ollama")
        .AddModel("gemma3n:e4b");

builder.AddProject<Projects.Brotal_FireflyBuddy_Web>("buddy-web")
    .WithReference(db)
    .WithReference(ollama);

builder.AddProject<Projects.Brotal_FireflyBuddy_Worker>("buddy-worker")
    .WithReference(db)
    .WithReference(ollama);

await builder.Build().RunAsync();
