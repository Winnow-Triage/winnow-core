using MassTransit;
using Winnow.Contracts;
using Winnow.Clustering;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Winnow.API.Extensions;
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

// Add MassTransit
builder.Services.AddWinnowMassTransit(builder.Configuration, builder.Environment,
    configureBus: x =>
    {
        x.AddConsumer<ClusteringBatchConsumer>();
    },
    configureFactory: (context, cfg) =>
    {
        if (cfg is IRabbitMqBusFactoryConfigurator rmq)
        {
            rmq.ReceiveEndpoint("cluster-reports-queue", e =>
            {
                e.PrefetchCount = 100;
                e.Batch<ReportSanitizedEvent>(b =>
                {
                    b.MessageLimit = 50;
                    b.TimeLimit = TimeSpan.FromSeconds(3);
                });

                e.ConfigureConsumer<ClusteringBatchConsumer>(context);
            });
        }
        else
        {
            // For InMemory, we can still configure the same endpoint logic if we want, 
            // but MassTransit InMemory usually handles it via ConfigureEndpoints if we don't need custom queue names.
            // However, Batching REQUIRES explicit endpoint configuration in some versions.
            cfg.ReceiveEndpoint("cluster-reports-queue", e =>
            {
                e.PrefetchCount = 100;
                e.Batch<ReportSanitizedEvent>(b =>
                {
                    b.MessageLimit = 50;
                    b.TimeLimit = TimeSpan.FromSeconds(3);
                });

                e.ConfigureConsumer<ClusteringBatchConsumer>(context);
            });
        }
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

    // Quick test — generate an embedding and check if it's deterministic
    var e1 = await embeddingService.GetEmbeddingAsync("test diagnostic");
    var e2 = await embeddingService.GetEmbeddingAsync("test diagnostic");
    var same = e1.Vector.SequenceEqual(e2.Vector);
    startupLogger.LogWarning("  Same text → same embedding? {Same} (should be True for ONNX, False for Mock)", same);
    startupLogger.LogWarning("=== END ONNX DIAGNOSTICS ===");
}

app.MapHealthChecks("/healthz");

await app.RunAsync();
