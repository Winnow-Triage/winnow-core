namespace Winnow.Server.Domain.ValueObjects;

public readonly record struct ConfidenceScore
{
    public double Value { get; }

    public ConfidenceScore(double value)
    {
        if (value < 0.0 || value > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Confidence score must be strictly between 0.0 and 1.0.");
        }

        Value = value;
    }
}