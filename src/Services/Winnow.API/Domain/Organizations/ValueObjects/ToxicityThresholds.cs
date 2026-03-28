namespace Winnow.API.Domain.Organizations.ValueObjects;

// Notice: No Id property! Just pure, immutable data.
public record ToxicityThresholds(
    float Profanity = 0.80f,
    float HateSpeech = 0.10f,
    float Violence = 0.10f,
    float Insult = 0.10f,
    float Harassment = 0.10f,
    float Sexual = 0.10f,
    float Graphic = 0.10f,
    float Overall = 0.10f)
{
    public static ToxicityThresholds Default => new();
}