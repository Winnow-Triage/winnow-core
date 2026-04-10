using Wolverine;
using Winnow.Sanitize;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Winnow.API.Extensions;
using Wolverine.AmazonSqs;
using Wolverine.RabbitMQ;


var builder = WebApplication.CreateBuilder(args);

// Add Health Checks
builder.Services.AddHealthChecks();

// Ensure Infrastructure Prerequisites (SSL Certificates, etc.)
builder.Configuration.EnsureRdsSslCertificate();

// Bind only needed infrastructure (Database, Shared Core, and Sanitize Services)
Console.WriteLine("ENTRY ASSEMBLY: " + System.Reflection.Assembly.GetEntryAssembly()?.FullName);
builder.Services.AddWinnowBaseInfrastructure(builder.Configuration);
builder.Services.AddWinnowSanitizeInfrastructure(builder.Configuration);

// Add Wolverine
builder.Host.UseWinnowWolverine(builder.Configuration, builder.Environment, enableOutbox: true, configure: opts =>
{
    opts.Discovery.IncludeAssembly(typeof(Winnow.Sanitize.AnalyzeReportHandler).Assembly);

    var projectName = builder.Configuration["ProjectName"] ?? "winnow";

    if (Environment.GetEnvironmentVariable("MESSAGE_BROKER")?.Equals("AmazonSqs", StringComparison.OrdinalIgnoreCase) == true)
    {
        opts.ListenToSqsQueue($"{projectName}-sanitize-queue");
    }
    else
    {
        opts.ListenToRabbitQueue($"{projectName}-sanitize-queue");
    }
});

var app = builder.Build();

app.MapHealthChecks("/healthz");

app.Run();
