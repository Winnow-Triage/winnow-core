namespace Winnow.Server.Domain.ValueObjects;

public readonly record struct AssetStatus
{
    public string Value { get; init; }

    private AssetStatus(string value) => Value = value;

    public static readonly AssetStatus Pending = new("Pending");
    public static readonly AssetStatus Clean = new("Clean");
    public static readonly AssetStatus Infected = new("Infected");
    public static readonly AssetStatus Failed = new("Failed");

    private static readonly HashSet<string> ValidValues = [Pending.Value, Clean.Value, Infected.Value, Failed.Value];

    public static AssetStatus From(string value)
    {
        if (!ValidValues.Contains(value))
            throw new ArgumentException($"'{value}' is not a valid asset status.", nameof(value));
        return new AssetStatus(value);
    }

    public override string ToString() => Value;
}
