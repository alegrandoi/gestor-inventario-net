namespace GestorInventario.Infrastructure.Configuration;

public static class DatabaseProviders
{
    public const string SqlServer = "SqlServer";
    public const string Sqlite = "Sqlite";
}

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string Provider { get; set; } = DatabaseProviders.SqlServer;

    public string? ConnectionString { get; set; }
}
