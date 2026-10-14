namespace Tezgah.Notifications;

public class NotificationsOptions
{
    public const string SectionName = "Notifications";

    /// <summary>İç API uçlarında X-Api-Key ile beklenen anahtar.</summary>
    public string ApiKey { get; set; } = "";
}

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
}

public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = "";
    public int Port { get; set; } = 25;
    public string From { get; set; } = "";
    public string FromName { get; set; } = "";
}
