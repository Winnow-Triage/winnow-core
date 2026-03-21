namespace Winnow.API.Domain.Assets.ValueObjects;

public readonly record struct AssetStatus
{
    public string Value { get; init; }

    private AssetStatus(string value) => Value = value;

    public static readonly AssetStatus Pending = new("Pending");
    public static readonly AssetStatus Clean = new("Clean");
    public static readonly AssetStatus Infected = new("Infected");
    public static readonly AssetStatus Failed = new("Failed");

    public static bool TryFromName(string? name, out AssetStatus? result)
    {
        result = name switch
        {
            "Pending" => Pending,
            "Clean" => Clean,
            "Infected" => Infected,
            "Failed" => Failed,
            _ => null
        };

        return result != null;
    }

    public static AssetStatus FromName(string name)
    {
        if (TryFromName(name, out var status))
        {
            return status!.Value;
        }

        throw new ArgumentException($"'{name}' is not a valid asset status.", nameof(name));
    }

    public static List<AssetStatus> List()
    {
        return [Pending, Clean, Infected, Failed];
    }

    public override string ToString() => Value;
}
