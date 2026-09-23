using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;
using AlmacenCloud.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlmacenCloud.Infrastructure.Identity;

public sealed class SmtpPasswordResetNotifier(
    IOptions<SmtpSettings> smtpOptions,
    IOptions<PasswordResetSettings> resetOptions,
    ILogger<SmtpPasswordResetNotifier> logger) : IPasswordResetNotifier
{
    public async Task SendAsync(string email, string name, string rawToken, CancellationToken cancellationToken)
    {
        var smtp = smtpOptions.Value;
        var reset = resetOptions.Value;
        if (string.IsNullOrWhiteSpace(smtp.Host) || string.IsNullOrWhiteSpace(smtp.FromAddress) ||
            string.IsNullOrWhiteSpace(reset.FrontendBaseUrl))
        {
            logger.LogWarning("Password recovery email was not sent because SMTP is not configured.");
            return;
        }

        var resetUrl = $"{reset.FrontendBaseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(rawToken)}";
        var safeName = HtmlEncoder.Default.Encode(name);
        var safeUrl = HtmlEncoder.Default.Encode(resetUrl);
        using var message = new MailMessage
        {
            From = new MailAddress(smtp.FromAddress, smtp.FromName),
            Subject = "Restablece tu contraseña de AlmacenCloud",
            IsBodyHtml = true,
            Body = $"<p>Hola {safeName},</p><p>Recibimos una solicitud para restablecer tu contraseña.</p><p><a href=\"{safeUrl}\">Crear una nueva contraseña</a></p><p>El enlace vence en 30 minutos y solo puede utilizarse una vez.</p><p>Si no realizaste la solicitud, ignora este mensaje.</p>"
        };
        message.To.Add(email);

        using var client = new SmtpClient(smtp.Host, smtp.Port) { EnableSsl = smtp.EnableSsl };
        if (!string.IsNullOrWhiteSpace(smtp.Username))
            client.Credentials = new NetworkCredential(smtp.Username, smtp.Password);
        try
        {
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (Exception exception) when (exception is SmtpException or InvalidOperationException)
        {
            // Preserve the same public response for existing and unknown emails.
            logger.LogError(exception, "Password recovery email delivery failed.");
        }
    }
}
