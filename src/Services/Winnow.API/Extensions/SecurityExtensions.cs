using Microsoft.AspNetCore.Identity;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Infrastructure.Identity;
using Winnow.API.Infrastructure.Security;
using Winnow.API.Infrastructure.Security.PoW;
using Winnow.API.Services.Quota;
using Winnow.API.Infrastructure.Billing;

namespace Winnow.API.Extensions;

internal static class SecurityExtensions
{
    public static IServiceCollection AddSecurityAndIdentity(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<IApiKeyService, ApiKeyService>();
        services.AddScoped<IQuotaService, QuotaService>();

        // Proof-of-Work
        services.Configure<PoWSettings>(config.GetSection("PoWSettings"));
        services.AddSingleton<IPoWValidator, PoWValidator>();

        // Stripe
        Stripe.StripeConfiguration.ApiKey = config["Stripe:SecretKey"];
        services.AddSingleton<IStripePlanMapper, StripePlanMapper>();

        // JWT Settings
        services.Configure<JwtSettings>(config.GetSection("JwtSettings"));
        services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<JwtSettings>>().Value);

        // Identity
        services.AddWinnowIdentity();

        // Authentication
        services.AddWinnowAuthentication(config);

        return services;
    }

    private static void AddWinnowIdentity(this IServiceCollection services)
    {
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 8;
            options.Password.RequiredUniqueChars = 1;
            options.User.RequireUniqueEmail = true;
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<WinnowDbContext>()
        .AddDefaultTokenProviders();
    }

    private static void AddWinnowAuthentication(this IServiceCollection services, IConfiguration config)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = "JwtOrApiKey";
            options.DefaultChallengeScheme = "JwtOrApiKey";
        })
        .AddPolicyScheme("JwtOrApiKey", "JWT or API Key", options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                string authHeader = context.Request.Headers["Authorization"]!;
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    return Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
                }

                return "ApiKey";
            };
        })
        .AddJwtBearer(options =>
        {
            var jwtSettings = config.GetSection("JwtSettings").Get<JwtSettings>()
                ?? throw new InvalidOperationException("JwtSettings configuration is missing");

            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                    System.Text.Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
            };
        })
        .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>("ApiKey", null);
    }
}
