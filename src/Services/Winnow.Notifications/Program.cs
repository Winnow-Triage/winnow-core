using Winnow.Notifications;
using Winnow.API.Extensions;
using Wolverine.AmazonSqs;

var builder = WebApplication.CreateBuilder(args);

// Add Health Checks
builder.Services.AddHealthChecks();

// Ensure Infrastructure Prerequisites (SSL Certificates, etc.)
builder.Configuration.EnsureRdsSslCertificate();

// Bind common infrastructure (Shared Core, Database, and Webhook Formatters)
builder.Services.AddWinnowBaseInfrastructure(builder.Configuration);

// Register Webhook Formatters
builder.Services.AddSingleton<IWebhookFormatter, DiscordWebhookFormatter>();
builder.Services.AddSingleton<IWebhookFormatter, SlackWebhookFormatter>();
builder.Services.AddSingleton<IWebhookFormatter, MicrosoftTeamsWebhookFormatter>();

// Configure Typed HttpClient for Webhooks with standard resilience
builder.Services.AddHttpClient("Webhooks")
    .AddStandardResilienceHandler();

// Configure Wolverine
builder.Host.UseWinnowWolverine(builder.Configuration, builder.Environment, enableOutbox: false, configure: opts =>
{
    opts.Discovery.IncludeAssembly(typeof(Winnow.Notifications.WebhookNotificationHandler).Assembly);

    var projectName = builder.Configuration["ProjectName"] ?? "winnow";
    opts.ListenToSqsQueue($"{projectName}-notifications-queue");
});

var app = builder.Build();

app.MapHealthChecks("/healthz");

app.Run();
