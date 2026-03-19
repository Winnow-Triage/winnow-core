using Winnow.Server.Domain.Organizations.ValueObjects;
using Winnow.Server.Domain.Reports.ValueObjects;

namespace Winnow.Server.Extensions;

public static class ToxicityMappingExtensions
{
    public static ToxicityPolicy ToPolicy(this ToxicityThresholds limits)
    {
        return new ToxicityPolicy(
            Overall: limits.Overall,
            Graphic: limits.Graphic,
            Profanity: limits.Profanity,
            HateSpeech: limits.HateSpeech,
            Harassment: limits.Harassment,
            Insult: limits.Insult,
            Violence: limits.Violence,
            Sexual: limits.Sexual
        );
    }
}