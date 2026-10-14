namespace Tezgah.Infrastructure.Messaging;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = default!;
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string VirtualHost { get; set; } = "/";
}
