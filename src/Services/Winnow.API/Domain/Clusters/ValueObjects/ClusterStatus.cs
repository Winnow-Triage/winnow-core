using Winnow.API.Domain.Common;

namespace Winnow.API.Domain.Clusters.ValueObjects;

public readonly record struct ClusterStatus
{
    public string Name { get; init; }
    public StatusCategory Category { get; init; }

    private ClusterStatus(string name, StatusCategory category)
    {
        Name = name;
        Category = category;
    }

    // --- THE TRIAGE LIFECYCLE ---

    // Action required by a human in Winnow
    public static readonly ClusterStatus Open = new("Open", StatusCategory.Active);

    // Terminal states (Winnow's job is finished)
    public static readonly ClusterStatus Exported = new("Exported", StatusCategory.Terminal); // Sent to Jira/Linear
    public static readonly ClusterStatus Dismissed = new("Dismissed", StatusCategory.Terminal); // Ignored / Won't Fix
    public static readonly ClusterStatus Merged = new("Merged", StatusCategory.Terminal); // Absorbed into another cluster

    // --- BEHAVIORS ---

    public bool IsActive() => Category == StatusCategory.Active;

    // Any logic that checks "Is the triage complete?" just calls this single method.
    public bool IsTerminal() => Category == StatusCategory.Terminal;

    // --- ENTITY FRAMEWORK / PARSING ---

    public static bool TryFromName(string? name, out ClusterStatus? result)
    {
        result = name?.ToLowerInvariant() switch
        {
            "open" => Open,
            "exported" => Exported,
            "dismissed" => Dismissed,
            "merged" => Merged,
            _ => null
        };

        return result != null;
    }

    public static ClusterStatus FromName(string name)
    {
        if (TryFromName(name, out var status))
        {
            return status!.Value;
        }

        throw new ArgumentException($"Unknown cluster status: {name}");
    }

    public static List<ClusterStatus> List()
    {
        return [Open, Exported, Dismissed, Merged];
    }

    public override string ToString() => Name;
}