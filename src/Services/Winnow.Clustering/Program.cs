using Wolverine;
using Winnow.Contracts;
using Winnow.Clustering;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Winnow.API.Extensions;
using Wolverine.AmazonSqs;

using System.Runtime.InteropServices;
using Microsoft.ML.OnnxRuntime;

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

// Bind only needed infrastructure (Database, Shared Core, and Clustering Services)
builder.Services.AddWinnowBaseInfrastructure(builder.Configuration);
builder.Services.AddWinnowClusteringInfrastructure(builder.Configuration);
builder.Services.AddHostedService<Winnow.Clustering.Infrastructure.Scheduling.ClusterRefinementJob>();

// Add Wolverine
builder.Host.UseWinnowWolverine(builder.Configuration, builder.Environment, enableOutbox: true, configure: opts =>
{
    opts.Discovery.IncludeAssembly(typeof(Winnow.Clustering.ClusteringBatchHandler).Assembly);

    var projectName = builder.Configuration["ProjectName"] ?? "winnow";
    opts.ListenToSqsQueue($"{projectName}-clustering-queue");
});

var app = builder.Build();

// Startup diagnostics — log what the ONNX provider sees
{
    var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    var contentRoot = app.Environment.ContentRootPath;
    var modelDir = Path.Combine(contentRoot, "AiModel");
    var modelPath = Path.Combine(modelDir, "model.onnx");
    var vocabPath = Path.Combine(modelDir, "vocab.txt");
    var nativeLib = Path.Combine(AppContext.BaseDirectory, "libonnxruntime.so");

    startupLogger.LogWarning("=== ONNX DIAGNOSTICS ===");
    startupLogger.LogWarning("  ContentRootPath: {Path}", contentRoot);
    startupLogger.LogWarning("  BaseDirectory:   {Path}", AppContext.BaseDirectory);
    startupLogger.LogWarning("  model.onnx:      {Exists} ({Path})", File.Exists(modelPath), modelPath);
    startupLogger.LogWarning("  vocab.txt:       {Exists} ({Path})", File.Exists(vocabPath), vocabPath);
    startupLogger.LogWarning("  libonnxruntime:  {Exists} ({Path})", File.Exists(nativeLib), nativeLib);

    // Check provider selection
    var embeddingService = app.Services.GetRequiredService<Winnow.API.Services.Ai.IEmbeddingService>();
    startupLogger.LogWarning("  EmbeddingService type: {Type}", embeddingService.GetType().Name);

    // Removed embedding validation test from startup. If embedding provider is misconfigured, 
    // it will throw gracefully at request-time instead of entering an endless bot startup CrashLoopBackOff.
    startupLogger.LogWarning("=== END ONNX DIAGNOSTICS ===");
}

app.MapHealthChecks("/healthz");

await app.RunAsync();
