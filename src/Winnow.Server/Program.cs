using FastEndpoints;
using FastEndpoints.Swagger;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Winnow.Integrations;
using Winnow.Server.Infrastructure.Configuration;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<Winnow.Server.Infrastructure.Integrations.ExporterFactory>();
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<Winnow.Server.Services.Ai.IEmbeddingService, Winnow.Server.Services.Ai.EmbeddingService>();
builder.Services.AddHostedService<Winnow.Server.Infrastructure.Scheduling.ClusterRefinementJob>();


var llmSettings = new Winnow.Server.Infrastructure.Configuration.LlmSettings();
builder.Configuration.GetSection("LlmSettings").Bind(llmSettings);
builder.Services.AddSingleton(llmSettings);

if (llmSettings.Provider == "Ollama")
{
    builder.Services.AddKernel();
    builder.Services.AddOllamaChatCompletion(
        modelId: llmSettings.Ollama.ModelId,
        endpoint: new Uri(llmSettings.Ollama.Endpoint));

    // Secondary model for fast gatekeeping (phi3/gemma)
    builder.Services.AddOllamaChatCompletion(
        serviceId: "Gatekeeper",
        modelId: llmSettings.Ollama.GatekeeperModelId,
        endpoint: new Uri(llmSettings.Ollama.Endpoint));

    builder.Services.AddScoped<Winnow.Server.Features.Tickets.GenerateSummary.IClusterSummaryService, Winnow.Server.Features.Tickets.GenerateSummary.SemanticKernelClusterSummaryService>();
}
else if (llmSettings.Provider == "OpenAI")
{
    builder.Services.AddKernel();
    builder.Services.AddOpenAIChatCompletion(
        modelId: llmSettings.OpenAI.ModelId,
        apiKey: llmSettings.OpenAI.ApiKey);
    builder.Services.AddScoped<Winnow.Server.Features.Tickets.GenerateSummary.IClusterSummaryService, Winnow.Server.Features.Tickets.GenerateSummary.SemanticKernelClusterSummaryService>();
}
else
{
    builder.Services.AddScoped<Winnow.Server.Features.Tickets.GenerateSummary.IClusterSummaryService, Winnow.Server.Features.Tickets.GenerateSummary.PlaceholderSummaryService>();
}

// Always register the duplicate checker (It handles fail-safe internally)
builder.Services.AddScoped<Winnow.Server.Services.Ai.IDuplicateChecker, Winnow.Server.Services.Ai.OllamaDuplicateChecker>();
builder.Services.AddSingleton<Winnow.Server.Services.Ai.INegativeMatchCache, Winnow.Server.Services.Ai.NegativeMatchCache>();

builder.Services.AddScoped<Winnow.Server.Features.Dashboard.IDashboardService, Winnow.Server.Features.Dashboard.DashboardService>();

builder.Services.AddDbContext<WinnowDbContext>(); // Configuration happens in OnConfiguring dynamically

builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument();
builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<Winnow.Server.Features.Tickets.Create.TicketCreatedConsumer>();
    x.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();
    db.Database.EnsureCreated();

    // SQLite multi-tenancy: Apply schema changes to ALL tenant databases
    var dataDir = Path.Combine(builder.Environment.ContentRootPath, "Data");
    if (Directory.Exists(dataDir))
    {
        var dbFiles = Directory.GetFiles(dataDir, "*.db");
        foreach (var dbFile in dbFiles)
        {
            var connectionString = $"Data Source={dbFile}";

            var optionsBuilder = new DbContextOptionsBuilder<WinnowDbContext>();
            optionsBuilder.UseSqlite(connectionString);

            using var tenantDb = new WinnowDbContext(optionsBuilder.Options, null!);

            try
            {
                tenantDb.Database.ExecuteSqlRaw("ALTER TABLE Tickets ADD COLUMN SuggestedParentId TEXT;");
            }
            catch { /* Column likely already exists */ }

            try
            {
                tenantDb.Database.ExecuteSqlRaw("ALTER TABLE Tickets ADD COLUMN SuggestedConfidenceScore REAL;");
            }
            catch { /* Column likely already exists */ }

            try
            {
                tenantDb.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS ""IntegrationConfigs"" (
                        ""Id"" TEXT NOT NULL CONSTRAINT ""PK_IntegrationConfigs"" PRIMARY KEY,
                        ""Provider"" TEXT NOT NULL,
                        ""SettingsJson"" TEXT NOT NULL,
                        ""IsActive"" INTEGER NOT NULL
                    );
                ");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create IntegrationConfigs table: {ex.Message}");
            }
        }
    }
}

app.UseMiddleware<TenantMiddleware>();

app.UseAuthorization();
app.UseCors();
app.UseFastEndpoints();
app.UseSwaggerGen();


app.Run();
