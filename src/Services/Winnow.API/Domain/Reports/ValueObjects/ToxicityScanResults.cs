namespace Winnow.API.Domain.Reports.ValueObjects;

// The actual scan results from AWS
public record ToxicityScanResult(
    float Overall,
    float Graphic,
    float HateSpeech,
    float Harassment,
    float Insult,
    float Profanity,
    float Sexual,
    float Violence)
{
    // THE BUSINESS RULE LIVES HERE!
    public bool Violates(ToxicityPolicy policy)
    {
        return Overall >= policy.Overall ||
               Profanity >= policy.Profanity ||
               HateSpeech >= policy.HateSpeech ||
               Insult >= policy.Insult ||
               Violence >= policy.Violence;
    }
}