using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Winnow.Server.Domain.Reports.ValueObjects;
using Winnow.Server.Infrastructure.Analysis;
using Winnow.Server.Infrastructure.Configuration;

namespace Winnow.Server.Tests.Unit.Services.Analysis;

public class AnalysisServiceTests
{
    private readonly Mock<IOptions<LlmSettings>> _optionsMock;
    private readonly LlmSettings _settings;
    private readonly Mock<ILogger<ToxicityDetectionService>> _toxicityLoggerMock;
    private readonly Mock<ILogger<PiiRedactionService>> _piiLoggerMock;

    public AnalysisServiceTests()
    {
        _settings = new LlmSettings();
        _optionsMock = new Mock<IOptions<LlmSettings>>();
        _optionsMock.Setup(o => o.Value).Returns(_settings);
        _toxicityLoggerMock = new Mock<ILogger<ToxicityDetectionService>>();
        _piiLoggerMock = new Mock<ILogger<PiiRedactionService>>();
    }

    [Fact]
    public async Task ToxicityDetectionService_SelectsProviderThatCanHandleSettings()
    {
        // Arrange
        _settings.ToxicityProvider = "AmazonComprehend";

        var provider1Mock = new Mock<IToxicityDetectionProvider>();
        provider1Mock.Setup(p => p.CanHandle(_settings)).Returns(false);

        var provider2Mock = new Mock<IToxicityDetectionProvider>();
        provider2Mock.Setup(p => p.CanHandle(_settings)).Returns(true);
        var expectedResult = new ToxicityScanResult(0.5f, 0, 0, 0, 0, 0, 0, 0);
        provider2Mock.Setup(p => p.DetectToxicityAsync("test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var service = new ToxicityDetectionService(
            new[] { provider1Mock.Object, provider2Mock.Object },
            _optionsMock.Object,
            _toxicityLoggerMock.Object);

        // Act
        var result = await service.DetectToxicityAsync("test");

        // Assert
        Assert.Equal(expectedResult, result);
        provider1Mock.Verify(p => p.CanHandle(_settings), Times.AtLeastOnce);
        provider2Mock.Verify(p => p.CanHandle(_settings), Times.AtLeastOnce);
        provider2Mock.Verify(p => p.DetectToxicityAsync("test", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ToxicityDetectionService_FallsBackToFirstProviderIfNoneCanHandle()
    {
        // Arrange
        _settings.ToxicityProvider = "Unknown";

        var provider1Mock = new Mock<IToxicityDetectionProvider>();
        provider1Mock.Setup(p => p.CanHandle(_settings)).Returns(false);
        var expectedResult = new ToxicityScanResult(0, 0, 0, 0, 0, 0, 0, 0);
        provider1Mock.Setup(p => p.DetectToxicityAsync("test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var service = new ToxicityDetectionService(
            new[] { provider1Mock.Object },
            _optionsMock.Object,
            _toxicityLoggerMock.Object);

        // Act
        var result = await service.DetectToxicityAsync("test");

        // Assert
        Assert.Equal(expectedResult, result);
        provider1Mock.Verify(p => p.DetectToxicityAsync("test", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PiiRedactionService_SelectsProviderThatCanHandleSettings()
    {
        // Arrange
        _settings.PiiRedactionProvider = "Local";

        var provider1Mock = new Mock<IPiiRedactionProvider>();
        provider1Mock.Setup(p => p.CanHandle(_settings)).Returns(true);
        provider1Mock.Setup(p => p.RedactPiiAsync("hello james", It.IsAny<CancellationToken>()))
            .ReturnsAsync("hello [PERSON]");

        var service = new PiiRedactionService(
            new[] { provider1Mock.Object },
            _optionsMock.Object,
            _piiLoggerMock.Object);

        // Act
        var result = await service.RedactPiiAsync("hello james");

        // Assert
        Assert.Equal("hello [PERSON]", result);
        provider1Mock.Verify(p => p.CanHandle(_settings), Times.AtLeastOnce);
    }

    [Fact]
    public async Task PiiRedactionService_ReturnsOriginalTextIfNoProvidersRegistered()
    {
        // Arrange
        var service = new PiiRedactionService(
            Enumerable.Empty<IPiiRedactionProvider>(),
            _optionsMock.Object,
            _piiLoggerMock.Object);

        // Act
        var result = await service.RedactPiiAsync("test text");

        // Assert
        Assert.Equal("test text", result);
    }
}
