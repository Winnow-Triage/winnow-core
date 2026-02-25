namespace Winnow.Server.Infrastructure.Configuration;

public class EmailSettings
{
    public string Provider { get; set; } = "Smtp"; // "Smtp" or "AwsSes"
    public string FromAddress { get; set; } = "no-reply@winnowtriage.com";
    public string FromName { get; set; } = "Winnow";

    public SmtpSettings Smtp { get; set; } = new();
    public AwsSesSettings AwsSes { get; set; } = new();
}

public class SmtpSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public bool EnableSsl { get; set; }
}

public class AwsSesSettings
{
    public string Region { get; set; } = "us-east-1";
}
