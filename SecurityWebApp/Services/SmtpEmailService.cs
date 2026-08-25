using System.Net;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace SecurityWebApp.Services;

public class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailSettings> settings, ILogger<SmtpEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (!_settings.IsConfigured)
        {
            throw new InvalidOperationException(
                "SMTP is not configured. Set Email:Host and Email:SenderEmail in appsettings.json, " +
                "and Email:Username / Email:Password with \"dotnet user-secrets\".");
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart(TextFormat.Html) { Text = htmlBody };

        using var client = new SmtpClient();
        var socketOptions = _settings.UseStartTls
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.SslOnConnect;

        await client.ConnectAsync(_settings.Host, _settings.Port, socketOptions, cancellationToken);

        if (!string.IsNullOrWhiteSpace(_settings.Username))
        {
            await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        _logger.LogInformation("Sent \"{Subject}\" to a registered address.", subject);
    }

    public Task SendPasswordResetCodeAsync(string toEmail, string resetCode, int expiresInMinutes, CancellationToken cancellationToken = default)
    {
        var code = WebUtility.HtmlEncode(resetCode);
        var body = $"""
            <p>Hello,</p>
            <p>We received a request to reset your Communication LTD password.
               Use the verification code below to continue:</p>
            <p style="font-family:Consolas,monospace;font-size:16px;letter-spacing:1px;
                      background:#f2f4f7;padding:12px 16px;border-radius:6px;display:inline-block;">
               {code}
            </p>
            <p>The code expires in {expiresInMinutes} minutes and can be used once.</p>
            <p>If you did not request a password reset you can ignore this message.</p>
            """;

        return SendAsync(toEmail, "Your Communication LTD password reset code", body, cancellationToken);
    }

    public Task SendWelcomeAsync(string toEmail, CancellationToken cancellationToken = default)
    {
        var body = """
            <p>Welcome to Communication LTD.</p>
            <p>Your account has been created and is ready to use.</p>
            """;

        return SendAsync(toEmail, "Welcome to Communication LTD", body, cancellationToken);
    }
}
