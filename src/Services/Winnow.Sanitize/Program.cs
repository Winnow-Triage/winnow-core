using MassTransit;
using Winnow.Sanitize;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Winnow.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Health Checks
builder.Services.AddHealthChecks();

// Bind only needed infrastructure (Database, Shared Core, and Sanitize Services)
builder.Services.AddWinnowBaseInfrastructure(builder.Configuration);
builder.Services.AddWinnowSanitizeInfrastructure(builder.Configuration);

// Add MassTransit
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<AnalyzeReportConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
        // Configure concurrent consumer execution
        cfg.PrefetchCount = 16;
        cfg.UseConcurrencyLimit(16);
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

app.MapHealthChecks("/healthz");

app.Run();
