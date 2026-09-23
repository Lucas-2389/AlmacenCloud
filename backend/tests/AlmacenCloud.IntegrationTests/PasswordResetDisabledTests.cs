using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AlmacenCloud.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AlmacenCloud.IntegrationTests;

public sealed class PasswordResetDisabledTests : IClassFixture<PasswordResetDisabledFactory>
{
    private readonly HttpClient _client;

    public PasswordResetDisabledTests(PasswordResetDisabledFactory factory) =>
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    [Fact]
    public async Task ProductionWithoutSmtp_StartsAndRejectsPasswordRecoverySecurely()
    {
        var health = await _client.GetAsync("/api/v1/health");
        var configuration = await _client.GetAsync("/api/v1/config/public");
        var forgot = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = "any@example.com" });

        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal(HttpStatusCode.OK, configuration.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, forgot.StatusCode);
        var json = await JsonDocument.ParseAsync(await configuration.Content.ReadAsStreamAsync());
        Assert.False(json.RootElement.GetProperty("passwordResetEnabled").GetBoolean());
    }
}

public sealed class PasswordResetDisabledFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Server=unused;Database=unused;User=unused;Password=unused;");
        builder.UseSetting("Jwt:Issuer", "AlmacenCloud.Tests");
        builder.UseSetting("Jwt:Audience", "AlmacenCloud.Tests.Client");
        builder.UseSetting("Jwt:Secret", "integration-tests-only-secret-with-at-least-32-bytes");
        builder.UseSetting("Jwt:ExpirationMinutes", "60");
        builder.UseSetting("PasswordReset:Enabled", "false");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AlmacenCloudDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AlmacenCloudDbContext>>();
            services.RemoveAll<AlmacenCloudDbContext>();
            services.AddDbContext<AlmacenCloudDbContext>(options => options.UseInMemoryDatabase($"PasswordResetDisabled-{Guid.NewGuid()}"));
        });
    }
}
