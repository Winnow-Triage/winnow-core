using Winnow.Server.Domain.Common;

namespace Winnow.Server.Domain.Reports.ValueObjects;

public readonly record struct ReportStatus
{
    public string Name { get; init; }
    public StatusCategory Category { get; init; }

    private ReportStatus(string name, StatusCategory category)
    {
        Name = name;
        Category = category;
    }

    // Active States (Needs routing or is actively being routed)
    public static readonly ReportStatus Open = new("Open", StatusCategory.Active); // Fresh, raw report
    public static readonly ReportStatus Duplicate = new("Duplicate", StatusCategory.Active); // Assigned to a cluster, actively being triaged

    // Terminal States (Winnow's job is done)
    public static readonly ReportStatus Resolved = new("Resolved", StatusCategory.Terminal); // Fixed upstream / Closed out
    public static readonly ReportStatus Dismissed = new("Dismissed", StatusCategory.Terminal); // Spam / Working as intended
    public static readonly ReportStatus Exported = new("Exported", StatusCategory.Terminal); // Sent to external issue tracker

    // --- BEHAVIORS ---

    public bool IsActive() => Category == StatusCategory.Active;
    public bool IsTerminal() => Category == StatusCategory.Terminal;

    // --- ENTITY FRAMEWORK / PARSING ---

    public static bool TryFromName(string? name, out ReportStatus? result)
    {
        result = name?.ToLowerInvariant() switch
        {
            "open" => Open,
            "duplicate" => Duplicate,
            "resolved" => Resolved,
            "dismissed" => Dismissed,
            _ => null
        };

        return result != null;
    }

    public static ReportStatus FromName(string name)
    {
        if (TryFromName(name, out var status))
        {
            return status!.Value;
        }

        throw new ArgumentException($"Unknown report status: {name}");
    }

    public static List<ReportStatus> List()
    {
        return [Open, Duplicate, Resolved, Dismissed, Exported];
    }

    public override string ToString() => Name;
}