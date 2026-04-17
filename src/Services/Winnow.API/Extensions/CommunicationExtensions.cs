using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Winnow.API.Infrastructure.Configuration;
using Winnow.API.Services.Emails;
using Winnow.API.Services.Discord;
using Amazon.SimpleEmail;
using Resend;

namespace Winnow.API.Extensions;

internal static class CommunicationExtensions
{
    public static IServiceCollection AddEmailAndNotifications(this IServiceCollection services, IConfiguration config)
    {
        var emailSettings = new EmailSettings();
        config.GetSection("EmailSettings").Bind(emailSettings);
        services.AddSingleton(emailSettings);

        // Discord
        services.Configure<DiscordOps>(config.GetSection("DiscordOps"));
        services.AddScoped<IInternalOpsNotifier, InternalOpsNotifier>();
        services.AddScoped<IClientNotificationService, ClientNotificationService>();

        if (emailSettings.Provider == "AwsSes")
        {
            services.AddAWSService<IAmazonSimpleEmailService>();
            services.AddScoped<IEmailService, AwsSesEmailService>();
        }
        else if (emailSettings.Provider == "Resend")
        {
            if (string.IsNullOrWhiteSpace(emailSettings.Resend.ApiKey))
                Console.WriteLine("[WARNING] Resend API Key is missing or empty. Emails will likely fail.");

            services.Configure<ResendClientOptions>(options => options.ApiToken = emailSettings.Resend.ApiKey);
            services.AddHttpClient<IResend, ResendClient>(client =>
                client.BaseAddress = new Uri(emailSettings.Resend.BaseUrl));
            services.AddScoped<IEmailService, ResendEmailService>();
        }
        else
        {
            services.AddScoped<IEmailService, SmtpEmailService>();
        }

        return services;
    }
}
