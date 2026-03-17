using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Winnow.Server.Extensions;
using Winnow.Server.Infrastructure.Security.Authorization;
using Winnow.Server.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// --- THE .NET 10.0.3 LINUX BYPASS ---
// Intercept the broken DllImport and force it to load the correct Linux library
// Wrapped in try-catch because SetDllImportResolver can only be called once per assembly.
// Integration tests create multiple WebApplicationFactory hosts which re-enter Program.Main.
try
{
    NativeLibrary.SetDllImportResolver(typeof(SessionOptions).Assembly, (libraryName, assembly, searchPath) =>
    {
        if (libraryName.Contains("onnxruntime"))
        {
            // Bypass the ".dll.so" nonsense and point directly to the native Linux asset
            string soPath = Path.Combine(AppContext.BaseDirectory, "bin/Debug/net10.0/libonnxruntime.so");

            if (File.Exists(soPath) && NativeLibrary.TryLoad(soPath, out IntPtr handle))
            {
                return handle;
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
builder.Services.AddWinnowServices(builder.Configuration);

var app = builder.Build();

// Configure global exception handling with custom middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure Winnow middleware pipeline
await app.UseWinnowMiddleware();

app.Run();

public partial class Program { }