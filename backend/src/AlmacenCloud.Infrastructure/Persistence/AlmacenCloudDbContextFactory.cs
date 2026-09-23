using AlmacenCloud.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AlmacenCloud.Infrastructure.Persistence;

public sealed class AlmacenCloudDbContextFactory : IDesignTimeDbContextFactory<AlmacenCloudDbContext>
{
    public AlmacenCloudDbContext CreateDbContext(string[] args)
    {
        var environment = GetEnvironment(args)
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Production";

        var configurationBuilder = new ConfigurationBuilder();
        if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
            configurationBuilder.AddUserSecrets<AlmacenCloudDbContextFactory>(optional: true);

        var configuration = configurationBuilder
            .AddEnvironmentVariables()
            .Build();
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "No se encontró ConnectionStrings:DefaultConnection. Configúrela mediante user-secrets en Development o la variable de entorno ConnectionStrings__DefaultConnection.");

        var options = new DbContextOptionsBuilder<AlmacenCloudDbContext>()
            .UseMySQL(connectionString)
            .Options;
        return new AlmacenCloudDbContext(options, new DesignTimeTenantContext());
    }

    private static string? GetEnvironment(string[] args)
    {
        for (var index = 0; index < args.Length; index++)
        {
            if ((args[index] is "--environment" or "-e") && index + 1 < args.Length)
                return args[index + 1];
            if (args[index].StartsWith("--environment=", StringComparison.OrdinalIgnoreCase))
                return args[index]["--environment=".Length..];
        }

        return null;
    }

    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public bool HasTenant => false;
        public Guid? EmpresaId => null;
    }
}
