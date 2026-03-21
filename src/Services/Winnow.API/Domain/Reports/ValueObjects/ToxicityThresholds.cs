namespace Winnow.API.Domain.Reports.ValueObjects;

public record ToxicityPolicy(
    float Overall,
    float Graphic,
    float HateSpeech,
    float Harassment,
    float Insult,
    float Profanity,
    float Sexual,
    float Violence);