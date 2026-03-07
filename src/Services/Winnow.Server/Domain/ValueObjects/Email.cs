using System.Text.RegularExpressions;

namespace Winnow.Server.Domain.ValueObjects;

public readonly record struct Email
{
#pragma warning disable SYSLIB1045 //Disable the warning for the regex compilation since we're not matching thousands of strings per second
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
#pragma warning restore SYSLIB1045

    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Email cannot be empty.", nameof(value));
        }

        if (!EmailRegex.IsMatch(value))
        {
            throw new ArgumentException($"'{value}' is not a valid email address.", nameof(value));
        }

        Value = value.ToLowerInvariant();
    }

    public override string ToString() => Value;
}