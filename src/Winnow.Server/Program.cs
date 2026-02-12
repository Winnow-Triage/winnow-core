using FastEndpoints;
using FastEndpoints.Swagger;
using MassTransit;
using Microsoft.SemanticKernel;
using Winnow.Integrations;
using Winnow.Server.Infrastructure.Configuration;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddTransient<ITicketExporter, TrelloExporter>();
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
app.UseMiddleware<TenantMiddleware>();

app.UseAuthorization();
app.UseCors();
app.UseFastEndpoints();
app.UseSwaggerGen();


app.Run();
