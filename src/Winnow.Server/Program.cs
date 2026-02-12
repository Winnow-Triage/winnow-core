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
builder.Services.AddTransient<ITicketExporter>(sp => sp.GetRequiredService<Winnow.Server.Infrastructure.Integrations.ExporterFactory>().GetExporter());
builder.Services.AddHttpClient();
builder.Services.AddSingleton<Winnow.Server.Services.Ai.IEmbeddingService, Winnow.Server.Services.Ai.EmbeddingService>();


var llmSettings = new Winnow.Server.Infrastructure.Configuration.LlmSettings();
builder.Configuration.GetSection("LlmSettings").Bind(llmSettings);
builder.Services.AddSingleton(llmSettings);

if (llmSettings.Provider == "Ollama")
{
    builder.Services.AddKernel();
    builder.Services.AddOllamaChatCompletion(
        modelId: llmSettings.Ollama.ModelId,
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
        }
    }
}

app.UseMiddleware<TenantMiddleware>();

app.UseAuthorization();
app.UseCors();
app.UseFastEndpoints();
app.UseSwaggerGen();


app.Run();
