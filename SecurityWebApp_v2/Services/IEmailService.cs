namespace SecurityWebApp.Services;

public interface IEmailService
{
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);

    Task SendPasswordResetCodeAsync(string toEmail, string resetCode, int expiresInMinutes, CancellationToken cancellationToken = default);

    Task SendWelcomeAsync(string toEmail, CancellationToken cancellationToken = default);
}
