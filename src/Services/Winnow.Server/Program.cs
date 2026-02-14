using FastEndpoints;
using FastEndpoints.Swagger;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Winnow.Integrations;
using Winnow.Server.Infrastructure.Configuration;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<Winnow.Server.Infrastructure.Integrations.ExporterFactory>();
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<Winnow.Server.Services.Ai.IEmbeddingService, Winnow.Server.Services.Ai.EmbeddingService>();
builder.Services.AddHostedService<Winnow.Server.Infrastructure.Scheduling.ClusterRefinementJob>();


var llmSettings = new Winnow.Server.Infrastructure.Configuration.LlmSettings();
builder.Configuration.GetSection("LlmSettings").Bind(llmSettings);
builder.Services.AddSingleton(llmSettings);

if (llmSettings.Provider == "Ollama")
{
    builder.Services.AddKernel();
    builder.Services.AddOllamaChatCompletion(
        modelId: llmSettings.Ollama.ModelId,
        endpoint: new Uri(llmSettings.Ollama.Endpoint));

    // Secondary model for fast gatekeeping (phi3/gemma)
    builder.Services.AddOllamaChatCompletion(
        serviceId: "Gatekeeper",
        modelId: llmSettings.Ollama.GatekeeperModelId,
        endpoint: new Uri(llmSettings.Ollama.Endpoint));

    builder.Services.AddScoped<Winnow.Server.Features.Reports.GenerateSummary.IClusterSummaryService, Winnow.Server.Features.Reports.GenerateSummary.SemanticKernelClusterSummaryService>();
}
else if (llmSettings.Provider == "OpenAI")
{
    builder.Services.AddKernel();
    builder.Services.AddOpenAIChatCompletion(
        modelId: llmSettings.OpenAI.ModelId,
        apiKey: llmSettings.OpenAI.ApiKey);
    builder.Services.AddScoped<Winnow.Server.Features.Reports.GenerateSummary.IClusterSummaryService, Winnow.Server.Features.Reports.GenerateSummary.SemanticKernelClusterSummaryService>();
}
else
{
    builder.Services.AddScoped<Winnow.Server.Features.Reports.GenerateSummary.IClusterSummaryService, Winnow.Server.Features.Reports.GenerateSummary.PlaceholderSummaryService>();
}

// Always register the duplicate checker (It handles fail-safe internally)
builder.Services.AddScoped<Winnow.Server.Services.Ai.IDuplicateChecker, Winnow.Server.Services.Ai.OllamaDuplicateChecker>();
builder.Services.AddSingleton<Winnow.Server.Services.Ai.INegativeMatchCache, Winnow.Server.Services.Ai.NegativeMatchCache>();

builder.Services.AddScoped<Winnow.Server.Features.Dashboard.IDashboardService, Winnow.Server.Features.Dashboard.DashboardService>();

builder.Services.AddDbContext<WinnowDbContext>(); // Configuration happens in OnConfiguring dynamically

builder.Services.AddIdentity<Winnow.Server.Entities.ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole>()
    .AddEntityFrameworkStores<WinnowDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? "super_secret_key_at_least_32_chars_long_for_safety"))
    };
});

builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument();
builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<Winnow.Server.Features.Reports.Create.ReportCreatedConsumer>();
    x.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();

    db.Database.Migrate();

    // SQLite multi-tenancy: Apply schema changes to ALL tenant databases
    var dataDir = Path.Combine(builder.Environment.ContentRootPath, "Data");
    if (Directory.Exists(dataDir))
    {
        var dbFiles = Directory.GetFiles(dataDir, "*.db");
        foreach (var dbFile in dbFiles)
        {
            var connectionString = $"Data Source={dbFile}";

            var optionsBuilder = new DbContextOptionsBuilder<WinnowDbContext>();
            optionsBuilder.UseSqlite(connectionString);

            using var tenantDb = new WinnowDbContext(optionsBuilder.Options, null!);

            try
            {
                tenantDb.Database.Migrate();

                // Seed a placeholder user first to avoid FK constraint failure
                tenantDb.Database.ExecuteSqlRaw(@"
                    INSERT OR IGNORE INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount, FullName, CreatedAt)
                    VALUES ('00000000-0000-0000-0000-000000000001', 'system@winnow.com', 'SYSTEM@WINNOW.COM', 'system@winnow.com', 1, 'AQAAAAIAAYagAAAAEJ...', 'stamp', 'stamp', 0, 0, 0, 0, 'System User', '2024-01-01');
                ");

                tenantDb.Database.ExecuteSqlRaw(@"
                    INSERT OR IGNORE INTO Projects (Id, Name, ApiKey, CreatedAt, OwnerId)
                    VALUES ('00000000-0000-0000-0000-000000000001', 'Default Project', 'secret-key', '2024-01-01', '00000000-0000-0000-0000-000000000001');
                ");

                // Ensure vec_reports exists (virtual tables are not in migrations)
                tenantDb.Database.ExecuteSqlRaw("CREATE VIRTUAL TABLE IF NOT EXISTS vec_reports USING vec0(embedding float[384] distance_metric=cosine);");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to migrate/seed tenant database {dbFile}: {ex.Message}");
            }
        }
    }
}

app.UseMiddleware<TenantMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();
app.UseCors();
app.UseFastEndpoints();
app.UseSwaggerGen();


app.Run();
