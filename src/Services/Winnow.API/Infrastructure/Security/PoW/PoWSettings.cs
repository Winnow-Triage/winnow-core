namespace Winnow.API.Infrastructure.Security.PoW;

public class PoWSettings
{
    public bool Enabled { get; set; } = true;
    public int Difficulty { get; set; } = 4;
    public int MaxTimestampAgeMinutes { get; set; } = 5;
}
