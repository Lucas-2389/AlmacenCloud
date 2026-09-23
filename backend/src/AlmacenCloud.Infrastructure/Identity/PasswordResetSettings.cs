namespace AlmacenCloud.Infrastructure.Identity;

public sealed class PasswordResetSettings
{
    public const string SectionName = "PasswordReset";
    public string FrontendBaseUrl { get; set; } = string.Empty;
}

public sealed class SmtpSettings
{
    public const string SectionName = "Smtp";
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "AlmacenCloud";
    public string? Username { get; set; }
    public string? Password { get; set; }
}
