using AlmacenCloud.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlmacenCloud.Infrastructure.Identity;

public sealed class DevelopmentPasswordResetNotifier(
    IOptions<PasswordResetSettings> options,
    ILogger<DevelopmentPasswordResetNotifier> logger) : IPasswordResetNotifier
{
    public Task SendAsync(string email, string name, string rawToken, CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled) return Task.CompletedTask;
        var url = $"{options.Value.FrontendBaseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(rawToken)}";
        logger.LogWarning("DEVELOPMENT ONLY - password reset link for {Email}: {ResetUrl}", email, url);
        return Task.CompletedTask;
    }
}
