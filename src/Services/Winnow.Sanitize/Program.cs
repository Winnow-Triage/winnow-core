using Wolverine;
using Winnow.Sanitize;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Winnow.API.Extensions;
using Wolverine.AmazonSqs;


var builder = WebApplication.CreateBuilder(args);

// Add Health Checks
builder.Services.AddHealthChecks();

// Bind only needed infrastructure (Database, Shared Core, and Sanitize Services)
builder.Services.AddWinnowBaseInfrastructure(builder.Configuration);
builder.Services.AddWinnowSanitizeInfrastructure(builder.Configuration);

// Add Wolverine
builder.Host.UseWinnowWolverine(builder.Configuration, builder.Environment, enableOutbox: true, configure: opts =>
{
    opts.Discovery.IncludeAssembly(typeof(Winnow.Sanitize.AnalyzeReportHandler).Assembly);

    var projectName = builder.Configuration["ProjectName"] ?? "winnow";
    opts.ListenToSqsQueue($"{projectName}-sanitize-queue");
});

var app = builder.Build();

app.MapHealthChecks("/healthz");

app.Run();
