using Xunit;

namespace Winnow.API.Tests.Integration;

/// <summary>
/// Skip test when running in CI environment.
/// </summary>
public sealed class SkipOnCIFactAttribute : FactAttribute
{
    public SkipOnCIFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("CI") != null)
        {
            Skip = "Test skipped in CI environment";
        }
    }
}