using FastEndpoints;
using FastEndpoints.Swagger;
using MassTransit;
using Winnow.Integrations;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddTransient<ITicketExporter, TrelloExporter>();
builder.Services.AddSingleton<Winnow.Server.Services.Ai.IEmbeddingService, Winnow.Server.Services.Ai.EmbeddingService>();

builder.Services.AddDbContext<WinnowDbContext>(); // Configuration happens in OnConfiguring dynamically

builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument();
builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<Winnow.Server.Features.Tickets.Create.TicketCreatedConsumer>();
    x.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMiddleware<TenantMiddleware>();

app.UseAuthorization();
app.UseCors();
app.UseFastEndpoints();
app.UseSwaggerGen();


app.Run();
