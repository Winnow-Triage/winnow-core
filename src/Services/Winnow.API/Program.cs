using Microsoft.ML.OnnxRuntime;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Winnow.API.Extensions;
using Winnow.API.Infrastructure.Security.Authorization;
using Winnow.API.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// --- THE .NET 10.0.3 LINUX BYPASS ---
// Intercept the broken DllImport and force it to load the correct Linux library
// Wrapped in try-catch because SetDllImportResolver can only be called once per assembly.
// Integration tests create multiple WebApplicationFactory hosts which re-enter Program.Main.
try
{
    NativeLibrary.SetDllImportResolver(typeof(Microsoft.ML.OnnxRuntime.SessionOptions).Assembly, (libraryName, assembly, searchPath) =>
    {
        if (libraryName.Contains("onnxruntime", StringComparison.OrdinalIgnoreCase))
        {
            // Check the published Docker directory first (flattened root)
            string publishedPath = Path.Combine(AppContext.BaseDirectory, "libonnxruntime.so");
            // Fallback for local development
            string localPath = Path.Combine(AppContext.BaseDirectory, "bin/Debug/net10.0/libonnxruntime.so");

            if (File.Exists(publishedPath) && NativeLibrary.TryLoad(publishedPath, out IntPtr handle))
            {
                return handle;
            }
            else if (File.Exists(localPath) && NativeLibrary.TryLoad(localPath, out IntPtr handle2))
            {
                return handle2;
            }
        }
        return IntPtr.Zero; // Fallback
    });
}
catch (InvalidOperationException)
{
    // Resolver already set for this assembly — safe to ignore
}

// Register all Winnow services
builder.Services.AddWinnowServices(builder.Configuration, builder.Environment);

builder.Host.UseWinnowWolverine(builder.Configuration, builder.Environment, enableOutbox: true);

var app = builder.Build();

// Configure global exception handling with custom middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure Winnow middleware pipeline
await app.UseWinnowMiddleware();

app.Run();

public partial class Program { }