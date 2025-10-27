namespace GestorInventario.Infrastructure.Configuration;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public bool Enabled { get; set; }
        = false;

    public string HostName { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string? Username { get; set; }
        = null;

    public string? Password { get; set; }
        = null;

    public string VirtualHost { get; set; } = "/";

    public string Exchange { get; set; } = "gestor-inventario";

    public string? ConnectionString { get; set; }
        = null;
}
