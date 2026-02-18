using System.Net.Http.Headers;
using System.Net.Http.Json;

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
        var response = await httpClient.PostAsJsonAsync(url, payload, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            // Log this content! Trello usually returns a helpful message like "invalid value for idList"
            throw new HttpRequestException($"Trello Export Failed ({response.StatusCode}): {content}");
        }

        var result = await response.Content.ReadFromJsonAsync<TrelloCardResponse>(cancellationToken: cancellationToken);
        return result?.ShortUrl ?? "https://trello.com";
    }

    private class TrelloCardResponse { public required string ShortUrl { get; set; } }
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
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"GitHub Export Failed ({response.StatusCode}): {content}");
        }

        var result = await response.Content.ReadFromJsonAsync<GitHubIssueResponse>(cancellationToken: cancellationToken);
        return result?.HtmlUrl ?? $"https://github.com/{owner}/{repo}/issues";
    }

    private class GitHubIssueResponse { [System.Text.Json.Serialization.JsonPropertyName("html_url")] public required string HtmlUrl { get; set; } }
}

public class JiraExporter(HttpClient httpClient, string baseUrl, string userEmail, string apiToken, string projectKey) : IReportExporter
{
    public async Task<string> ExportReportAsync(string title, string description, CancellationToken cancellationToken)
    {
        var authValue = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{userEmail}:{apiToken}"));
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);

        var response = await httpClient.PostAsJsonAsync(
            $"{baseUrl.TrimEnd('/')}/rest/api/3/issue",
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
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Jira Export Failed ({response.StatusCode}): {content}");
        }

        var result = await response.Content.ReadFromJsonAsync<JiraIssueResponse>(cancellationToken: cancellationToken);
        return $"{baseUrl.TrimEnd('/')}/browse/{result?.Key}";
    }

    private class JiraIssueResponse { public required string Key { get; set; } }
}
