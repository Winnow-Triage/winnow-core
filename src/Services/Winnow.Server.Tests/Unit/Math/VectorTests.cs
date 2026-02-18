using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Winnow.Server.Infrastructure.Scheduling;

namespace Winnow.Server.Tests.Unit.Math;

public class VectorTests
{
    private readonly ClusterRefinementJob _job;
    private readonly MethodInfo _calculateCosineDistanceMethod;
    private readonly MethodInfo _calculateCentroidMethod;
    private readonly MethodInfo _bytesToFloatsMethod;

    public VectorTests()
    {
        // Create an instance of ClusterRefinementJob using a mock scope factory and logger
        var scopeFactory = new Mock<IServiceScopeFactory>().Object;
        var logger = new Mock<ILogger<ClusterRefinementJob>>().Object;
        _job = new ClusterRefinementJob(scopeFactory, logger);

        // Get private methods via reflection
        var type = typeof(ClusterRefinementJob);
        _calculateCosineDistanceMethod = type.GetMethod("CalculateCosineDistance", BindingFlags.NonPublic | BindingFlags.Instance)!;
        _calculateCentroidMethod = type.GetMethod("CalculateCentroid", BindingFlags.NonPublic | BindingFlags.Instance)!;
        _bytesToFloatsMethod = type.GetMethod("BytesToFloats", BindingFlags.NonPublic | BindingFlags.Instance)!;
    }

    [Fact]
    public void CalculateCosineDistance_IdenticalVectors_ReturnsZeroDistance()
    {
        // Arrange
        float[] vector = [1.0f, 2.0f, 3.0f, 4.0f];

        // Act
        var distance = (double)_calculateCosineDistanceMethod.Invoke(_job, [vector, vector])!;

        // Assert
        // Cosine distance between identical vectors should be 0 (cosine similarity = 1)
        Assert.Equal(0.0, distance, precision: 10);
    }

    [Fact]
    public void CalculateCosineDistance_OrthogonalVectors_ReturnsHalfDistance()
    {
        // Arrange
        float[] vector1 = [1.0f, 0.0f, 0.0f];
        float[] vector2 = [0.0f, 1.0f, 0.0f];

        // Act
        var distance = (double)_calculateCosineDistanceMethod.Invoke(_job, [vector1, vector2])!;

        // Assert
        // Cosine similarity between orthogonal vectors is 0, so distance = 1 - 0 = 1
        Assert.Equal(1.0, distance, precision: 10);
    }

    [Fact]
    public void CalculateCosineDistance_OppositeVectors_ReturnsMaxDistance()
    {
        // Arrange
        float[] vector1 = [1.0f, 2.0f, 3.0f];
        float[] vector2 = [-1.0f, -2.0f, -3.0f];

        // Act
        var distance = (double)_calculateCosineDistanceMethod.Invoke(_job, [vector1, vector2])!;

        // Assert
        // Cosine similarity between opposite vectors is -1, so distance = 1 - (-1) = 2
        // But cosine distance is typically clamped between 0 and 2, with 2 being maximum distance
        Assert.Equal(2.0, distance, precision: 10);
    }

    [Fact]
    public void CalculateCosineDistance_SameDirectionDifferentMagnitude_ReturnsSmallDistance()
    {
        // Arrange
        float[] vector1 = [1.0f, 2.0f, 3.0f];
        float[] vector2 = [2.0f, 4.0f, 6.0f]; // Same direction, twice the magnitude

        // Act
        var distance = (double)_calculateCosineDistanceMethod.Invoke(_job, [vector1, vector2])!;

        // Assert
        // Cosine similarity should be 1 (same direction), so distance should be 0
        Assert.Equal(0.0, distance, precision: 10);
    }

    [Fact]
    public void CalculateCosineDistance_EmptyFirstVector_ReturnsOne()
    {
        // Arrange
        float[] emptyVector = [];
        float[] vector = [1.0f, 2.0f, 3.0f];

        // Act
        var distance = (double)_calculateCosineDistanceMethod.Invoke(_job, [emptyVector, vector])!;

        // Assert
        Assert.Equal(1.0, distance);
    }

    [Fact]
    public void CalculateCosineDistance_EmptySecondVector_ReturnsOne()
    {
        // Arrange
        float[] vector = [1.0f, 2.0f, 3.0f];
        float[] emptyVector = [];

        // Act
        var distance = (double)_calculateCosineDistanceMethod.Invoke(_job, [vector, emptyVector])!;

        // Assert
        Assert.Equal(1.0, distance);
    }

    [Fact]
    public void CalculateCosineDistance_BothEmptyVectors_ReturnsOne()
    {
        // Arrange
        float[] emptyVector1 = [];
        float[] emptyVector2 = [];

        // Act
        var distance = (double)_calculateCosineDistanceMethod.Invoke(_job, [emptyVector1, emptyVector2])!;

        // Assert
        Assert.Equal(1.0, distance);
    }

    [Fact]
    public void CalculateCosineDistance_ZeroVector_ReturnsOne()
    {
        // Arrange
        float[] zeroVector = [0.0f, 0.0f, 0.0f];
        float[] vector = [1.0f, 2.0f, 3.0f];

        // Act
        var distance = (double)_calculateCosineDistanceMethod.Invoke(_job, [zeroVector, vector])!;

        // Assert
        Assert.Equal(1.0, distance);
    }

    [Fact]
    public void CalculateCosineDistance_BothZeroVectors_ReturnsOne()
    {
        // Arrange
        float[] zeroVector1 = [0.0f, 0.0f, 0.0f];
        float[] zeroVector2 = [0.0f, 0.0f, 0.0f];

        // Act
        var distance = (double)_calculateCosineDistanceMethod.Invoke(_job, [zeroVector1, zeroVector2])!;

        // Assert
        Assert.Equal(1.0, distance);
    }

    [Fact]
    public void CalculateCosineDistance_VectorsWithNegativeValues_CalculatesCorrectly()
    {
        // Arrange
        float[] vector1 = [1.0f, -2.0f, 3.0f];
        float[] vector2 = [-1.0f, 2.0f, -3.0f];

        // Act
        var distance = (double)_calculateCosineDistanceMethod.Invoke(_job, [vector1, vector2])!;

        // Assert
        // Manual calculation:
        // dot = (1*-1) + (-2*2) + (3*-3) = -1 + -4 + -9 = -14
        // ma = 1^2 + (-2)^2 + 3^2 = 1 + 4 + 9 = 14
        // mb = (-1)^2 + 2^2 + (-3)^2 = 1 + 4 + 9 = 14
        // similarity = -14 / (sqrt(14) * sqrt(14)) = -14 / 14 = -1
        // distance = 1 - (-1) = 2
        Assert.Equal(2.0, distance, precision: 10);
    }

    [Fact]
    public void CalculateCosineDistance_DifferentLengthVectors_ThrowsException()
    {
        // Arrange
        float[] vector1 = [1.0f, 2.0f, 3.0f];
        float[] vector2 = [1.0f, 2.0f]; // Different length

        // Act & Assert
        var exception = Assert.Throws<TargetInvocationException>(() =>
            _calculateCosineDistanceMethod.Invoke(_job, [vector1, vector2]));

        // The actual exception will be IndexOutOfRangeException when the method tries to access vector2[2]
        Assert.IsType<IndexOutOfRangeException>(exception.InnerException);
    }

    [Fact]
    public void CalculateCentroid_SingleEmbedding_ReturnsSameVector()
    {
        // Arrange
        var embeddings = new List<byte[]?>
        {
            CreateFloatBytes([1.0f, 2.0f, 3.0f])
        };

        // Act
        var centroid = (float[])_calculateCentroidMethod.Invoke(_job, [embeddings])!;

        // Assert
        Assert.Equal([1.0f, 2.0f, 3.0f], centroid);
    }

    [Fact]
    public void CalculateCentroid_MultipleEmbeddings_ReturnsAverage()
    {
        // Arrange
        var embeddings = new List<byte[]?>
        {
            CreateFloatBytes([1.0f, 2.0f, 3.0f]),
            CreateFloatBytes([3.0f, 4.0f, 5.0f]),
            CreateFloatBytes([5.0f, 6.0f, 7.0f])
        };

        // Act
        var centroid = (float[])_calculateCentroidMethod.Invoke(_job, [embeddings])!;

        // Assert
        // Average of (1,3,5)=3, (2,4,6)=4, (3,5,7)=5
        Assert.Equal([3.0f, 4.0f, 5.0f], centroid);
    }

    [Fact]
    public void CalculateCentroid_WithNullEmbeddings_IgnoresNulls()
    {
        // Arrange
        var embeddings = new List<byte[]?>
        {
            CreateFloatBytes([1.0f, 2.0f, 3.0f]),
            null,
            CreateFloatBytes([5.0f, 6.0f, 7.0f]),
            null
        };

        // Act
        var centroid = (float[])_calculateCentroidMethod.Invoke(_job, [embeddings])!;

        // Assert
        // Average of (1,5)=3, (2,6)=4, (3,7)=5
        Assert.Equal([3.0f, 4.0f, 5.0f], centroid);
    }

    [Fact]
    public void CalculateCentroid_AllNullEmbeddings_ReturnsEmptyArray()
    {
        // Arrange
        var embeddings = new List<byte[]?> { null, null, null };

        // Act
        var centroid = (float[])_calculateCentroidMethod.Invoke(_job, [embeddings])!;

        // Assert
        Assert.Empty(centroid);
    }

    [Fact]
    public void CalculateCentroid_EmptyList_ReturnsEmptyArray()
    {
        // Arrange
        var embeddings = new List<byte[]?>();

        // Act
        var centroid = (float[])_calculateCentroidMethod.Invoke(_job, [embeddings])!;

        // Assert
        Assert.Empty(centroid);
    }

    [Fact]
    public void BytesToFloats_ConvertsCorrectly()
    {
        // Arrange
        float[] expectedFloats = [1.0f, 2.0f, 3.0f, 4.0f];
        byte[] bytes = new byte[expectedFloats.Length * sizeof(float)];
        Buffer.BlockCopy(expectedFloats, 0, bytes, 0, bytes.Length);

        // Act
        var result = (float[])_bytesToFloatsMethod.Invoke(_job, [bytes])!;

        // Assert
        Assert.Equal(expectedFloats, result);
    }

    [Fact]
    public void BytesToFloats_EmptyArray_ReturnsEmptyFloatArray()
    {
        // Arrange
        byte[] emptyBytes = [];

        // Act
        var result = (float[])_bytesToFloatsMethod.Invoke(_job, [emptyBytes])!;

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void CalculateCosineDistance_IntegrationTest_WithRealisticEmbeddings()
    {
        // Arrange - typical 384-dimensional embeddings
        float[] embedding1 = [.. Enumerable.Range(0, 384).Select(i => (float)System.Math.Sin(i * 0.1f))];
        float[] embedding2 = [.. Enumerable.Range(0, 384).Select(i => (float)System.Math.Cos(i * 0.1f))];

        // Normalize to unit vectors for predictable results
        float norm1 = (float)System.Math.Sqrt(embedding1.Sum(x => x * x));
        float norm2 = (float)System.Math.Sqrt(embedding2.Sum(x => x * x));
        for (int i = 0; i < 384; i++)
        {
            embedding1[i] /= norm1;
            embedding2[i] /= norm2;
        }

        // Act
        var distance = (double)_calculateCosineDistanceMethod.Invoke(_job, [embedding1, embedding2])!;

        // Assert
        // Distance should be between 0 and 2
        Assert.InRange(distance, 0.0, 2.0);

        // For orthogonal-ish vectors, distance should be close to 1
        // Since sin and cos are orthogonal functions, their dot product over many samples should be near 0
        Assert.InRange(distance, 0.9, 1.1);
    }

    private static byte[] CreateFloatBytes(float[] floats)
    {
        byte[] bytes = new byte[floats.Length * sizeof(float)];
        Buffer.BlockCopy(floats, 0, bytes, 0, bytes.Length);
        return bytes;
    }
}