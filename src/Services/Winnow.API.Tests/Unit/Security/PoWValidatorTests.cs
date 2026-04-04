using Moq;
using Winnow.API.Infrastructure.Security.PoW;
using Winnow.API.Services.Caching;
using Xunit;

namespace Winnow.API.Tests.Unit.Security;

public class PoWValidatorTests
{
    private readonly PoWValidator _validator;
    private readonly Mock<ICacheService> _cacheMock;

    public PoWValidatorTests()
    {
        _cacheMock = new Mock<ICacheService>();
        _validator = new PoWValidator(_cacheMock.Object);
    }

    [Fact]
    public void Verify_ValidNonce_ReturnsTrue()
    {
        // Data mined for SHA256(test-keyPOST/reports2026-03-28T15:00:00Z65025)
        // Result: 0000bf2a...
        var apiKey = "test-key";
        var method = "POST";
        var path = "/reports";
        var timestamp = "2026-03-28T15:00:00Z";
        var nonce = "65025";
        var difficulty = 4;

        var result = _validator.Verify(apiKey, method, path, timestamp, nonce, difficulty);

        Assert.True(result);
    }

    [Fact]
    public void Verify_InvalidNonce_ReturnsFalse()
    {
        var apiKey = "test-key";
        var method = "POST";
        var path = "/reports";
        var timestamp = "2026-03-28T15:00:00Z";
        var nonce = "invalid-nonce";
        var difficulty = 4;

        var result = _validator.Verify(apiKey, method, path, timestamp, nonce, difficulty);

        Assert.False(result);
    }

    [Fact]
    public void Verify_DifferentDifficulty_ReturnsExpectedResult()
    {
        var apiKey = "test-key";
        var method = "POST";
        var path = "/reports";
        var timestamp = "2026-03-28T15:00:00Z";
        var nonce = "65025"; // Mined for difficulty 4

        // Should pass for difficulty 4
        Assert.True(_validator.Verify(apiKey, method, path, timestamp, nonce, 4));

        // Might pass or fail for difficulty 5 depending on the mined nonce, 
        // but it's guaranteed to pass for difficulty 0-4
        Assert.True(_validator.Verify(apiKey, method, path, timestamp, nonce, 2));
    }

    [Fact]
    public void Verify_CaseInsensitivity_ReturnsTrue()
    {
        var apiKey = "test-key";
        var method = "post"; // lower case
        var path = "/REPORTS"; // upper case
        var timestamp = "2026-03-28T15:00:00Z";
        var nonce = "65025";

        // Validator should normalize internally
        var result = _validator.Verify(apiKey, method, path, timestamp, nonce, 4);

        Assert.True(result);
    }
}
