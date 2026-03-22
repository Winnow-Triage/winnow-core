using MassTransit;
using Winnow.Summary;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using global::Winnow.API.Extensions;
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

// Bind only needed infrastructure (Database, Shared Core, and Summary Services)
builder.Services.AddWinnowBaseInfrastructure(builder.Configuration);
builder.Services.AddWinnowSummaryInfrastructure(builder.Configuration);
builder.Services.AddHostedService<Winnow.Summary.Infrastructure.Scheduling.CriticalMassSummaryJob>();

// Add MassTransit
builder.Services.AddWinnowMassTransit(builder.Configuration, builder.Environment,
    configureBus: x =>
    {
        x.AddConsumer<GenerateClusterSummaryConsumer>();
    },
    configureFactory: (context, cfg) =>
    {
        // Configure concurrent consumer execution
        cfg.PrefetchCount = 4;
        cfg.UseConcurrencyLimit(4);
    });

var app = builder.Build();

app.MapHealthChecks("/healthz");

app.Run();
