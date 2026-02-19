using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Winnow.Integrations;

public interface IReportExporter
{
    Task<string> ExportReportAsync(string title, string description, CancellationToken cancellationToken);
}

public class TrelloExporter(HttpClient httpClient, string apiKey, string token, string listId) : IReportExporter
{
    public async Task<string> ExportReportAsync(string title, string description, CancellationToken cancellationToken)
    {
        // 1. Keep Auth in the URL (Standard Trello practice)
        var url = $"https://api.trello.com/1/cards?key={apiKey}&token={token}";

        // 2. Put the Data in the Body (No length limits, safer encoding)
        var payload = new
        {
            idList = listId,
            name = title,
            desc = description,
            pos = "top" // Optional: Puts new bugs at the top of the list
        };

        // 3. Send as JSON
        var response = await httpClient.PostAsJsonAsync(url, payload, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            // Log this content! Trello usually returns a helpful message like "invalid value for idList"
            throw new HttpRequestException($"Trello Export Failed ({response.StatusCode}): {content}");
        }

        var result = await response.Content.ReadFromJsonAsync<TrelloCardResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
        return result?.ShortUrl?.AbsoluteUri ?? "https://trello.com";
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization")]
    private sealed record TrelloCardResponse(Uri ShortUrl);
}

public class GitHubExporter(HttpClient httpClient, string apiKey, string owner, string repo) : IReportExporter
{
    public async Task<string> ExportReportAsync(string title, string description, CancellationToken cancellationToken)
    {
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Winnow-App");
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await httpClient.PostAsJsonAsync(
            $"https://api.github.com/repos/{owner}/{repo}/issues",
            new { title, body = description },
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException($"GitHub Export Failed ({response.StatusCode}): {content}");
        }

        var result = await response.Content.ReadFromJsonAsync<GitHubIssueResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
        return result?.HtmlUrl?.AbsoluteUri ?? $"https://github.com/{owner}/{repo}/issues";
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization")]
    private sealed record GitHubIssueResponse([property: JsonPropertyName("html_url")] Uri HtmlUrl);
}

public class JiraExporter(HttpClient httpClient, Uri baseUrl, string userEmail, string apiToken, string projectKey) : IReportExporter
{
    public async Task<string> ExportReportAsync(string title, string description, CancellationToken cancellationToken)
    {
        var authValue = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{userEmail}:{apiToken}"));
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);

        var response = await httpClient.PostAsJsonAsync(
            $"{baseUrl.AbsoluteUri.TrimEnd('/')}/rest/api/3/issue",
            new
            {
                fields = new
                {
                    project = new { key = projectKey },
                    summary = title,
                    description = new
                    {
                        type = "doc",
                        version = 1,
                        content = new[]
                        {
                            new
                            {
                                type = "paragraph",
                                content = new[] { new { type = "text", text = description } }
                            }
                        }
                    },
                    issuetype = new { name = "Bug" }
                }
            },
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException($"Jira Export Failed ({response.StatusCode}): {content}");
        }

        var result = await response.Content.ReadFromJsonAsync<JiraIssueResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
        return $"{baseUrl.AbsoluteUri.TrimEnd('/')}/browse/{result?.Key}";
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization")]
    private sealed record JiraIssueResponse(string Key);
}
