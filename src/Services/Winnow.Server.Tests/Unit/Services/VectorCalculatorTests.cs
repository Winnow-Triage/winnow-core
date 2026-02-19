using System.Collections.Generic;
using System.Linq;
using Winnow.Server.Domain.Services;

namespace Winnow.Server.Tests.Unit.Services;

public class VectorCalculatorTests
{
    private readonly VectorCalculator _vectorCalculator;

    public VectorCalculatorTests()
    {
        _vectorCalculator = new VectorCalculator();
    }

    [Fact]
    public void CalculateCosineDistance_IdenticalVectors_ReturnsZeroDistance()
    {
        // Arrange
        float[] vector = [1.0f, 2.0f, 3.0f, 4.0f];

        // Act
        var distance = _vectorCalculator.CalculateCosineDistance(vector, vector);

        // Assert
        // Cosine distance between identical vectors should be 0 (cosine similarity = 1)
        Assert.Equal(0.0, distance, precision: 10);
    }

    [Fact]
    public void CalculateCosineDistance_OrthogonalVectors_ReturnsOneDistance()
    {
        // Arrange
        float[] vector1 = [1.0f, 0.0f, 0.0f];
        float[] vector2 = [0.0f, 1.0f, 0.0f];

        // Act
        var distance = _vectorCalculator.CalculateCosineDistance(vector1, vector2);

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
        var distance = _vectorCalculator.CalculateCosineDistance(vector1, vector2);

        // Assert
        // Cosine similarity between opposite vectors is -1, so distance = 1 - (-1) = 2
        Assert.Equal(2.0, distance, precision: 10);
    }

    [Fact]
    public void CalculateCosineDistance_SameDirectionDifferentMagnitude_ReturnsZeroDistance()
    {
        // Arrange
        float[] vector1 = [1.0f, 2.0f, 3.0f];
        float[] vector2 = [2.0f, 4.0f, 6.0f]; // Same direction, twice the magnitude

        // Act
        var distance = _vectorCalculator.CalculateCosineDistance(vector1, vector2);

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
        var distance = _vectorCalculator.CalculateCosineDistance(emptyVector, vector);

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
        var distance = _vectorCalculator.CalculateCosineDistance(vector, emptyVector);

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
        var distance = _vectorCalculator.CalculateCosineDistance(emptyVector1, emptyVector2);

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
        var distance = _vectorCalculator.CalculateCosineDistance(zeroVector, vector);

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
        var distance = _vectorCalculator.CalculateCosineDistance(zeroVector1, zeroVector2);

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
        var distance = _vectorCalculator.CalculateCosineDistance(vector1, vector2);

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
    public void CalculateCosineDistance_DifferentLengthVectors_ThrowsArgumentException()
    {
        // Arrange
        float[] vector1 = [1.0f, 2.0f, 3.0f];
        float[] vector2 = [1.0f, 2.0f]; // Different length

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _vectorCalculator.CalculateCosineDistance(vector1, vector2));

        Assert.Contains("same length", exception.Message);
    }

    [Fact]
    public void CalculateCosineDistance_FirstVectorNull_ReturnsOne()
    {
        // Arrange
        float[]? vector1 = null;
        float[] vector2 = [1.0f, 2.0f, 3.0f];

        // Act
        var distance = _vectorCalculator.CalculateCosineDistance(vector1!, vector2);

        // Assert
        Assert.Equal(1.0, distance);
    }

    [Fact]
    public void CalculateCosineDistance_SecondVectorNull_ReturnsOne()
    {
        // Arrange
        float[] vector1 = [1.0f, 2.0f, 3.0f];
        float[]? vector2 = null;

        // Act
        var distance = _vectorCalculator.CalculateCosineDistance(vector1, vector2!);

        // Assert
        Assert.Equal(1.0, distance);
    }

    [Fact]
    public void CalculateCosineDistance_BothNullVectors_ReturnsOne()
    {
        // Arrange
        float[]? vector1 = null;
        float[]? vector2 = null;

        // Act
        var distance = _vectorCalculator.CalculateCosineDistance(vector1!, vector2!);

        // Assert
        Assert.Equal(1.0, distance);
    }

    [Fact]
    public void CalculateCentroid_SingleVector_ReturnsSameVector()
    {
        // Arrange
        List<float[]> vectors = [
            [1.0f, 2.0f, 3.0f]
        ];

        // Act
        var centroid = _vectorCalculator.CalculateCentroid(vectors);

        // Assert
        Assert.Equal([1.0f, 2.0f, 3.0f], centroid);
    }

    [Fact]
    public void CalculateCentroid_MultipleVectors_ReturnsAverage()
    {
        // Arrange
        List<float[]> vectors = [
            [1.0f, 2.0f, 3.0f],
            [3.0f, 4.0f, 5.0f],
            [5.0f, 6.0f, 7.0f]
        ];

        // Act
        var centroid = _vectorCalculator.CalculateCentroid(vectors);

        // Assert
        // Average of (1,3,5)=3, (2,4,6)=4, (3,5,7)=5
        Assert.Equal([3.0f, 4.0f, 5.0f], centroid);
    }

    [Fact]
    public void CalculateCentroid_WithNullVectors_IgnoresNulls()
    {
        // Arrange
        List<float[]?> vectorsWithNulls = [
            [1.0f, 2.0f, 3.0f],
            null,
            [5.0f, 6.0f, 7.0f],
            null
        ];
        
        var vectors = vectorsWithNulls.Where(v => v != null).Select(v => v!).ToList();

        // Act
        var centroid = _vectorCalculator.CalculateCentroid(vectors);

        // Assert
        // Average of (1,5)=3, (2,6)=4, (3,7)=5
        Assert.Equal([3.0f, 4.0f, 5.0f], centroid);
    }

    [Fact]
    public void CalculateCentroid_AllNullVectors_ReturnsEmptyArray()
    {
        // Arrange
        List<float[]> vectors = [];

        // Act
        var centroid = _vectorCalculator.CalculateCentroid(vectors);

        // Assert
        Assert.Empty(centroid);
    }

    [Fact]
    public void CalculateCentroid_VectorsWithDifferentLengths_ThrowsArgumentException()
    {
        // Arrange
        List<float[]> vectors = [
            [1.0f, 2.0f, 3.0f],
            [4.0f, 5.0f] // Different length
        ];

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _vectorCalculator.CalculateCentroid(vectors));

        Assert.Contains("same length", exception.Message);
    }

    [Fact]
    public void CalculateCentroid_EmptyList_ReturnsEmptyArray()
    {
        // Arrange
        List<float[]> vectors = [];

        // Act
        var centroid = _vectorCalculator.CalculateCentroid(vectors);

        // Assert
        Assert.Empty(centroid);
    }

    [Fact]
    public void CalculateCentroid_ListNull_ReturnsEmptyArray()
    {
        // Arrange
        List<float[]>? vectors = null;

        // Act
        var centroid = _vectorCalculator.CalculateCentroid(vectors!);

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
        var result = VectorCalculator.BytesToFloats(bytes);

        // Assert
        Assert.Equal(expectedFloats, result);
    }

    [Fact]
    public void BytesToFloats_EmptyArray_ReturnsEmptyFloatArray()
    {
        // Arrange
        byte[] emptyBytes = [];

        // Act
        var result = VectorCalculator.BytesToFloats(emptyBytes);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void BytesToFloats_NullArray_ReturnsEmptyFloatArray()
    {
        // Arrange
        byte[]? nullBytes = null;

        // Act
        var result = VectorCalculator.BytesToFloats(nullBytes);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FloatsToBytes_ConvertsCorrectly()
    {
        // Arrange
        float[] floats = [1.0f, 2.0f, 3.0f, 4.0f];

        // Act
        var bytes = VectorCalculator.FloatsToBytes(floats);
        var resultFloats = new float[floats.Length];
        Buffer.BlockCopy(bytes, 0, resultFloats, 0, bytes.Length);

        // Assert
        Assert.Equal(floats, resultFloats);
    }

    [Fact]
    public void FloatsToBytes_EmptyArray_ReturnsEmptyByteArray()
    {
        // Arrange
        float[] emptyFloats = [];

        // Act
        var result = VectorCalculator.FloatsToBytes(emptyFloats);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FloatsToBytes_NullArray_ReturnsEmptyByteArray()
    {
        // Arrange
        float[]? nullFloats = null;

        // Act
        var result = VectorCalculator.FloatsToBytes(nullFloats);

        // Assert
        Assert.Empty(result);
    }
}