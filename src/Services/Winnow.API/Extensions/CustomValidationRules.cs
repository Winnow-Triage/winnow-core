using System.Text.RegularExpressions;
using FluentValidation;

namespace Winnow.API.Extensions;

public static class CustomValidationRules
{
    private static readonly Regex ModelIdRegex = new("^[a-zA-Z0-9_.-]+$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(250));

    public static IRuleBuilderOptions<T, string?> MustBeValidFilePath<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder.Must(path =>
        {
            if (string.IsNullOrWhiteSpace(path)) return true; // Let NotEmpty handle if it's required

            // Prevent basic path traversal
            if (path.Contains("..")) return false;

            // Prevent absolute paths or rooting
            if (path.StartsWith('/') || path.StartsWith('\\')) return false;
            if (path.Contains(':')) return false; // Windows drive letters

            // Disallow null bytes
            if (path.Contains('\0')) return false;

            return true;
        }).WithMessage("'{PropertyName}' contains invalid characters or path traversal markers.");
    }

    public static IRuleBuilderOptions<T, string?> MustBeSecureUrl<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder.Must(urlStr =>
        {
            if (string.IsNullOrWhiteSpace(urlStr)) return true;

            if (!Uri.TryCreate(urlStr, UriKind.Absolute, out var uri))
                return false;

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return false;

            // Basic SSRF protection
            if (uri.IsLoopback || uri.Host.Contains("localhost") || uri.Host.StartsWith("127."))
                return false;

            return true;
        }).WithMessage("'{PropertyName}' must be a valid, secure external HTTP or HTTPS URL.");
    }

    public static IRuleBuilderOptions<T, string?> MustBeValidModelId<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder.Must(modelId =>
        {
            if (string.IsNullOrWhiteSpace(modelId)) return true;
            return ModelIdRegex.IsMatch(modelId);
        }).WithMessage("'{PropertyName}' can only contain alphanumeric characters, hyphens, periods, and underscores.");
    }
}
