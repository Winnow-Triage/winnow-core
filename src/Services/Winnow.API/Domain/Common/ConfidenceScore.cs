namespace Winnow.API.Domain.Common;


public readonly record struct ConfidenceScore
{
    public double Score { get; }

    public ConfidenceScore(double score)
    {
        if (score < 0.0 || score > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(score), "Confidence score must be strictly between 0.0 and 1.0.");
        }

        Score = score;
    }
}