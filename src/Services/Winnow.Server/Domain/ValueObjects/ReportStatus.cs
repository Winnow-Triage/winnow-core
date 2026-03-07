namespace Winnow.Server.Domain.ValueObjects;

public readonly record struct ReportStatus
{
    public string Value { get; init; }

    private ReportStatus(string value) => Value = value;

    public static readonly ReportStatus New = new("New");
    public static readonly ReportStatus Reviewed = new("Reviewed");
    public static readonly ReportStatus Dismissed = new("Dismissed");

    private static readonly HashSet<string> ValidValues = [New.Value, Reviewed.Value, Dismissed.Value];

    public static ReportStatus From(string value)
    {
        if (!ValidValues.Contains(value))
            throw new ArgumentException($"'{value}' is not a valid report status.", nameof(value));
        return new ReportStatus(value);
    }

    public override string ToString() => Value;
}
