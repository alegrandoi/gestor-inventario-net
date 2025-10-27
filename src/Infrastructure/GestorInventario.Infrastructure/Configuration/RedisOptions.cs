namespace GestorInventario.Infrastructure.Configuration;

public class RedisOptions
{
    public const string SectionName = "Redis";

    public bool Enabled { get; set; } = false;

    public string ConnectionString { get; set; } = string.Empty;

    public string InstanceName { get; set; } = "GestorInventario";
}
