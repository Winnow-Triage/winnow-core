namespace Winnow.Server.Domain.Services;

/// <summary>
/// Interface for vector calculation operations used in similarity matching.
/// </summary>
internal interface IVectorCalculator
{
    /// <summary>
    /// Calculates the centroid (average) of a list of vectors.
    /// </summary>
    /// <param name="vectors">List of vectors to average.</param>
    /// <returns>The centroid vector, or an empty array if no valid vectors are provided.</returns>
    float[] CalculateCentroid(List<float[]> vectors);

    /// <summary>
    /// Calculates the cosine distance between two vectors.
    /// </summary>
    /// <param name="v1">First vector.</param>
    /// <param name="v2">Second vector.</param>
    /// <returns>Cosine distance where 0 = identical, 1 = orthogonal, 2 = opposite.</returns>
    double CalculateCosineDistance(float[] v1, float[] v2);
}
