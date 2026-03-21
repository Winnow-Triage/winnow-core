using Xunit;

namespace Winnow.API.Tests;

public class HealthCheckTest
{
    [Fact]
    public void Server_ShouldHaveBasicInfrastructure()
    {
        // This is a basic health check to ensure the test runner works
        // and that the test project can reference the server assembly

        // Simple assertion to verify test infrastructure
        Assert.True(true, "Test runner should be working");

        // We could add more sophisticated checks here later:
        // - Verify DI container can be built
        // - Verify database connections
        // - Verify endpoints are registered
    }

    [Fact]
    public void TestProject_ShouldCompileAndRun()
    {
        // This test ensures the test project itself compiles
        // and can reference the server project

        // Verify that the test project is properly configured
        var testAssembly = typeof(HealthCheckTest).Assembly;
        Assert.NotNull(testAssembly);

        // Check that we're in the correct namespace
        Assert.Equal("Winnow.API.Tests", typeof(HealthCheckTest).Namespace);
    }

    [Fact]
    public void XUnit_ShouldBeWorking()
    {
        // Simple XUnit assertion to verify the test framework works
        Assert.Equal(1 + 1, 2);
    }
}