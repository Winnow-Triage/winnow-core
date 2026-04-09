using Wolverine;
using Winnow.Summary;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using global::Winnow.API.Extensions;
using System.Runtime.InteropServices;
using Microsoft.ML.OnnxRuntime;
using Wolverine.AmazonSqs;

var builder = WebApplication.CreateBuilder(args);

// Ensure ONNX runtime native library can be found in the container root
NativeLibrary.SetDllImportResolver(typeof(Microsoft.ML.OnnxRuntime.SessionOptions).Assembly, (libraryName, assembly, searchPath) =>
{
    if (libraryName.Contains("onnxruntime", StringComparison.OrdinalIgnoreCase))
    {
        // Check the published Docker directory first (flattened root)
        string publishedPath = Path.Combine(AppContext.BaseDirectory, "libonnxruntime.so");
        if (File.Exists(publishedPath))
        {
            return NativeLibrary.Load(publishedPath);
        }
    }
    return IntPtr.Zero;
});

// Add Health Checks
builder.Services.AddHealthChecks();

// Ensure Infrastructure Prerequisites (SSL Certificates, etc.)
builder.Configuration.EnsureRdsSslCertificate();

// Bind only needed infrastructure (Database, Shared Core, and Summary Services)
builder.Services.AddWinnowBaseInfrastructure(builder.Configuration);
builder.Services.AddWinnowSummaryInfrastructure(builder.Configuration);
builder.Services.AddHostedService<Winnow.Summary.Infrastructure.Scheduling.CriticalMassSummaryJob>();

// Add Wolverine
builder.Host.UseWinnowWolverine(builder.Configuration, builder.Environment, enableOutbox: true, configure: opts =>
{
    opts.Discovery.IncludeAssembly(typeof(Winnow.Summary.GenerateClusterSummaryHandler).Assembly);

    var projectName = builder.Configuration["ProjectName"] ?? "winnow";
    opts.ListenToSqsQueue($"{projectName}-summary-queue");
});

var app = builder.Build();

app.MapHealthChecks("/healthz");

app.Run();
