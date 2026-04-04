using MassTransit;
using Winnow.Notifications;
using Winnow.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Health Checks
builder.Services.AddHealthChecks();

// Bind common infrastructure (Shared Core, Database, and Webhook Formatters)
builder.Services.AddWinnowBaseInfrastructure(builder.Configuration);

// Register Webhook Formatters
builder.Services.AddSingleton<IWebhookFormatter, DiscordWebhookFormatter>();
builder.Services.AddSingleton<IWebhookFormatter, SlackWebhookFormatter>();
builder.Services.AddSingleton<IWebhookFormatter, MicrosoftTeamsWebhookFormatter>();

// Configure Typed HttpClient for Webhooks with standard resilience
builder.Services.AddHttpClient("Webhooks")
    .AddStandardResilienceHandler();

// Configure MassTransit using centralized logic
builder.Services.AddWinnowMassTransit(builder.Configuration, builder.Environment,
    configureBus: x =>
    {
        x.AddConsumer<WebhookNotificationConsumer>();
    },
    configureFactory: (context, cfg) =>
    {
        cfg.PrefetchCount = 16;
    });

var app = builder.Build();

app.MapHealthChecks("/healthz");

app.Run();
