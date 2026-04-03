using System;
using System.Threading.Tasks;
using Winnow.Sdk.DotNet.Core;
using Winnow.Sdk.DotNet.Core.Models;

namespace WinnowTester;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Winnow C# SDK Tester ===");
        
        string apiKey = "test-api-key";
        var config = new WinnowConfig 
        { 
            BaseUrl = "https://api.winnowtriage.com", // Updated default
            Environment = "Testing"
        };

        var client = new WinnowSdkClient(apiKey, config);

        var payload = new ReportPayload
        {
            Title = "Headless Test Report",
            Message = "Testing the refactored .NET Core SDK logic.",
            Platform = "Console",
            AppVersion = "1.0.0"
        };

        Console.WriteLine($"Sending report to {config.BaseUrl}...");

        try
        {
            await client.SendReportAsync(payload);
            Console.WriteLine("Success: Report sent (or attempted).");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Result: {ex.Message}");
            Console.WriteLine("(Note: api.winnowtriage.com may not be resolvable in this environment)");
        }

        Console.WriteLine("Test complete.");
    }
}
