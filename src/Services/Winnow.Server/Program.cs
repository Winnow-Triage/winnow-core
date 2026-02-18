using Winnow.Server.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register all Winnow services
builder.Services.AddWinnowServices(builder.Configuration);

var app = builder.Build();

// Configure Winnow middleware pipeline
await app.UseWinnowMiddleware();

app.Run();

public partial class Program { }
