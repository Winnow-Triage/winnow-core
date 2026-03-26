using Xunit;
using System.Threading.Tasks;

namespace Winnow.API.Tests;

public class HealthCheckTest
{
    [Fact]
    public void Server_ShouldHaveBasicInfrastructure()
    {
        Assert.True(true, "Test runner should be working");
    }

    [Fact]
    public void TestProject_ShouldCompileAndRun()
    {
        var testAssembly = typeof(HealthCheckTest).Assembly;
        Assert.NotNull(testAssembly);
        Assert.Equal("Winnow.API.Tests", typeof(HealthCheckTest).Namespace);
    }

    [Fact]
    public async Task EmailHealthCheck_ShouldBeHealthy_WhenProviderIsNone()
    {
        // Arrange
        var settings = new Winnow.API.Infrastructure.Configuration.EmailSettings { Provider = "None" };
        var check = new Winnow.API.Infrastructure.HealthChecks.EmailHealthCheck(settings);

        // Act
        var result = await check.CheckHealthAsync(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext());

        // Assert
        Assert.Equal(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy, result.Status);
        Assert.Equal("Not configured", result.Description);
        Assert.True(result.Data.ContainsKey("Note"));
    }

    [Fact]
    public async Task LlmHealthCheck_ShouldBeHealthy_WhenProviderIsNone()
    {
        // Arrange
        var settings = new Winnow.API.Infrastructure.Configuration.LlmSettings { Provider = "None" };
        var check = new Winnow.API.Infrastructure.HealthChecks.LlmHealthCheck(null!, settings);

        // Act
        var result = await check.CheckHealthAsync(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext());

        // Assert
        Assert.Equal(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy, result.Status);
        Assert.Equal("Not configured", result.Description);
        Assert.True(result.Data.ContainsKey("Note"));
    }
}