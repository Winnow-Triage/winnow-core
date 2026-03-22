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
builder.Services.AddWinnowMassTransit(builder.Configuration, builder.Environment, enableOutbox: true,
    configureBus: x =>
    {
        x.AddConsumer<AnalyzeReportConsumer>();
    },
    configureFactory: (context, cfg) =>
    {
        // Configure concurrent consumer execution
        cfg.PrefetchCount = 16;
        cfg.UseConcurrencyLimit(16);
    });

var app = builder.Build();

app.MapHealthChecks("/healthz");

app.Run();
