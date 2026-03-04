namespace Winnow.Server.Domain.Services;

/// <summary>
/// Implementation of vector calculation operations.
/// </summary>
public class VectorCalculator : IVectorCalculator
{
    /// <summary>
    /// Converts a byte array of floats to a float array.
    /// </summary>
    /// <param name="bytes">Byte array containing float values.</param>
    /// <returns>Float array representation, or empty array if input is null or empty.</returns>
    public static float[] BytesToFloats(byte[]? bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return [];

        float[] floats = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
        return floats;
    }

    /// <summary>
    /// Converts a float array to a byte array.
    /// </summary>
    /// <param name="floats">Float array to convert.</param>
    /// <returns>Byte array representation, or empty array if input is null or empty.</returns>
    public static byte[] FloatsToBytes(float[]? floats)
    {
        if (floats == null || floats.Length == 0)
            return [];

        byte[] bytes = new byte[floats.Length * sizeof(float)];
        Buffer.BlockCopy(floats, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    /// <inheritdoc />
    public float[] CalculateCentroid(List<float[]> vectors)
    {
        if (vectors == null || vectors.Count == 0)
            return [];

        // Filter out null vectors
        var validVectors = vectors.Where(v => v != null && v.Length > 0).ToList();

        if (validVectors.Count == 0)
            return [];

        // Verify all vectors have the same length
        int length = validVectors[0].Length;
        if (validVectors.Any(v => v.Length != length))
            throw new ArgumentException("All vectors must have the same length", nameof(vectors));

        float[] centroid = new float[length];

        // Sum all vectors
        foreach (var vector in validVectors)
        {
            for (int i = 0; i < length; i++)
            {
                centroid[i] += vector[i];
            }
        }

        // Divide by count to get average
        for (int i = 0; i < length; i++)
        {
            centroid[i] /= validVectors.Count;
        }

        return centroid;
    }

    /// <inheritdoc />
    public double CalculateCosineDistance(float[] v1, float[] v2)
    {
        if (!AreVectorsValid(v1, v2, out var errorResult))
            return errorResult;

        var (dot, ma, mb) = CalculateDotAndMagnitudes(v1, v2);

        if (ma == 0 || mb == 0)
            return 1.0;

        return 1.0 - (dot / (Math.Sqrt(ma) * Math.Sqrt(mb)));
    }

    private static bool AreVectorsValid(float[]? v1, float[]? v2, out double errorResult)
    {
        errorResult = 1.0;

        if (v1 == null || v2 == null)
            return false;

        if (v1.Length == 0 || v2.Length == 0)
            return false;

        if (v1.Length != v2.Length)
            throw new ArgumentException("Vectors must have the same length", nameof(v2));

        return true;
    }

    private static (float Dot, float Ma, float Mb) CalculateDotAndMagnitudes(float[] v1, float[] v2)
    {
        float dot = 0, ma = 0, mb = 0;

        for (int i = 0; i < v1.Length; i++)
        {
            float v1i = v1[i];
            float v2i = v2[i];

            dot += v1i * v2i;
            ma += v1i * v1i;
            mb += v2i * v2i;
        }

        return (dot, ma, mb);
    }
}
