namespace Winnow.API.Infrastructure.Configuration;

public class DiscordOps
{
    public Uri? NewSignupsUrl { get; set; }
    public Uri? DlqAlertsUrl { get; set; }
    public Uri? StripePaymentsUrl { get; set; }
}
