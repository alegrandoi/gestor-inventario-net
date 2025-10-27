using GestorInventario.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace GestorInventario.Api.Tests;

public class TestingWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(service =>
                service.ServiceType == typeof(DbContextOptions<GestorInventarioDbContext>));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            connection?.Dispose();
            connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            services.AddDbContext<GestorInventarioDbContext>(options =>
            {
                options.UseSqlite(connection);
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
        {
            return;
        }

        connection?.Dispose();
        connection = null;
    }
}
