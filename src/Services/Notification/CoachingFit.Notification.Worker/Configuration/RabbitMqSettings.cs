namespace CoachingFit.Notification.Worker.Configuration;

/// <summary>
/// Strongly-typed RabbitMQ connection settings, bound from the "RabbitMq"
/// configuration section. Defaults match our local docker-compose broker.
/// In production these are overridden by environment variables / secrets.
/// </summary>
public sealed class RabbitMqSettings
{
    public const string SectionName = "RabbitMq";

    public string Host { get; set; } = "localhost";
    public ushort Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
}
