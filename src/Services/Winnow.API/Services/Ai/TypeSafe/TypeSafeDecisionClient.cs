namespace Winnow.API.Services.Ai.TypeSafe;

public class TypeSafeDecisionClient(HttpClient httpClient, ILogger<TypeSafeDecisionClient> logger) : ITypeSafeDecisionClient
{
    public async Task<JevDecisionResponse?> EvaluateAsync(JevDecisionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Sending decision request to TypeSafe Jev API. Model: {ModelId}", request.Model);
            var response = await httpClient.PostAsJsonAsync("", request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError("Failed to get decision from TypeSafe Jev API. Status: {StatusCode}, Error: {ErrorContent}", response.StatusCode, errorContent);
                return null;
            }
            var result = await response.Content.ReadFromJsonAsync<JevDecisionResponse>(cancellationToken: cancellationToken);
            if (result?.Usage != null)
            {
                logger.LogInformation("Decision received. Cost: {Cost}, Input Tokens: {InputTokens}", result.Usage.Cost, result.Usage.InputTokens);
            }
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while calling TypeSafe Jev API.");
            return null;
        }
    }
}
