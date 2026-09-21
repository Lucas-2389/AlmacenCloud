using AlmacenCloud.Application.Abstractions;
using AlmacenCloud.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace AlmacenCloud.MySqlIntegrationTests;

public sealed class MySqlTestDatabase : IAsyncLifetime
{
    public const string RequiredDatabaseName = "almacencloud_tests";
    private readonly string? _connectionString;
    private readonly string? _unsafeReason;

    public MySqlTestDatabase()
    {
        _connectionString = LoadConnectionString();

        if (!string.IsNullOrWhiteSpace(_connectionString))
        {
            try
            {
                var parsed = new MySqlConnectionStringBuilder(_connectionString);
                if (!string.Equals(parsed.Database, RequiredDatabaseName, StringComparison.OrdinalIgnoreCase))
                    _unsafeReason = $"Protección activada: la base debe llamarse exactamente '{RequiredDatabaseName}', no '{parsed.Database}'.";
            }
            catch (Exception exception)
            {
                _unsafeReason = $"La conexión MySQL de tests no es válida: {exception.Message}";
            }
        }
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    public static string? LoadConnectionString()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<MySqlTestDatabase>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        return Environment.GetEnvironmentVariable("ALMACENCLOUD_TEST_MYSQL_CONNECTION")
            ?? configuration.GetConnectionString("MySqlTests");
    }

    public string RequireConnection()
    {
        if (_unsafeReason is not null) throw new InvalidOperationException(_unsafeReason);
        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new InvalidOperationException("Configure ALMACENCLOUD_TEST_MYSQL_CONNECTION o el user-secret ConnectionStrings:MySqlTests para ejecutar MySQL.");
        return _connectionString;
    }

    public AlmacenCloudDbContext CreateContext(Guid? empresaId = null)
    {
        var options = new DbContextOptionsBuilder<AlmacenCloudDbContext>().UseMySQL(RequireConnection()).Options;
        return new AlmacenCloudDbContext(options, new FixedTenantContext(empresaId));
    }

    public async Task ResetAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    private sealed class FixedTenantContext(Guid? empresaId) : ITenantContext
    {
        public bool HasTenant => empresaId.HasValue;
        public Guid? EmpresaId => empresaId;
    }
}
