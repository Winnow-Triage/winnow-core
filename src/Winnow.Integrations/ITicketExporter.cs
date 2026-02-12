using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Winnow.Integrations;

public interface ITicketExporter
{
    Task ExportTicketAsync(string title, string description, CancellationToken cancellationToken);
}

public class TrelloExporter(HttpClient httpClient, string apiKey, string token, string listId) : ITicketExporter
{
    public async Task ExportTicketAsync(string title, string description, CancellationToken cancellationToken)
    {
        var url = $"https://api.trello.com/1/cards?idList={listId}&key={apiKey}&token={token}&name={Uri.EscapeDataString(title)}&desc={Uri.EscapeDataString(description)}";
        var response = await httpClient.PostAsync(url, null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

public class GitHubExporter(HttpClient httpClient, string apiKey, string owner, string repo) : ITicketExporter
{
    public async Task ExportTicketAsync(string title, string description, CancellationToken cancellationToken)
    {
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Winnow-App");
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await httpClient.PostAsJsonAsync(
            $"https://api.github.com/repos/{owner}/{repo}/issues",
            new { title, body = description },
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }
}

public class JiraExporter(HttpClient httpClient, string baseUrl, string userEmail, string apiToken, string projectKey) : ITicketExporter
{
    public async Task ExportTicketAsync(string title, string description, CancellationToken cancellationToken)
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

        response.EnsureSuccessStatusCode();
    }
}
