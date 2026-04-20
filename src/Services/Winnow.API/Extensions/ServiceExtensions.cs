using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Winnow.API.Extensions;

internal static class ServiceExtensions
{
    public static IServiceCollection AddWinnowServices(this IServiceCollection services, IConfiguration config, IHostEnvironment hostEnv)
    {
        services.AddInfrastructureServices(config);
        services.AddWinnowHealthChecks();
        services.AddAiAndLlmServices(config);
        services.AddSecurityAndIdentity(config);
        services.AddEmailAndNotifications(config);
        services.AddMiddlewareAndPolicies(config);

        return services;
    }
}