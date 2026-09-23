namespace AlmacenCloud.Application.Abstractions;

public interface IPasswordResetNotifier
{
    Task SendAsync(string email, string name, string rawToken, CancellationToken cancellationToken);
}
