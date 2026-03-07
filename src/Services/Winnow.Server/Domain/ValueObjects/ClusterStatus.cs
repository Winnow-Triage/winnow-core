namespace Winnow.Server.Domain.ValueObjects;

public readonly record struct ClusterStatus
{
    public string Value { get; init; }

    private ClusterStatus(string value) => Value = value;

    public static readonly ClusterStatus Open = new("Open");
    public static readonly ClusterStatus Resolved = new("Resolved");
    public static readonly ClusterStatus Dismissed = new("Dismissed");

    private static readonly HashSet<string> ValidValues = [Open.Value, Resolved.Value, Dismissed.Value];

    public static ClusterStatus From(string value)
    {
        if (!ValidValues.Contains(value))
            throw new ArgumentException($"'{value}' is not a valid cluster status.", nameof(value));
        return new ClusterStatus(value);
    }

    public override string ToString() => Value;
}
