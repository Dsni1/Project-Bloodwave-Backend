using System.Net;
using System.Net.Mail;

namespace Project_Bloodwave_Backend.Services;

public interface IMailService
{
    Task<MailSendResult> SendEmailAsync(
        string to,
        string subject,
        string text,
        string? html = null,
        CancellationToken cancellationToken = default);
}

public sealed class SmtpMailService : IMailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpMailService> _logger;

    public SmtpMailService(IConfiguration configuration, ILogger<SmtpMailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<MailSendResult> SendEmailAsync(
        string to,
        string subject,
        string text,
        string? html = null,
        CancellationToken cancellationToken = default)
    {
        var host = _configuration["Smtp:Host"];
        var fromEmail = _configuration["Smtp:FromEmail"];
        var fromName = _configuration["Smtp:FromName"];
        var username = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];
        var useSsl = bool.TryParse(_configuration["Smtp:UseSsl"], out var ssl) && ssl;
        var useAuth = bool.TryParse(_configuration["Smtp:UseAuthentication"], out var auth) && auth;
        var port = int.TryParse(_configuration["Smtp:Port"], out var parsedPort) ? parsedPort : 25;

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromEmail))
        {
            const string error = "SMTP configuration is incomplete. Required: Smtp:Host and Smtp:FromEmail.";
            _logger.LogWarning(error);
            return MailSendResult.Failure(error);
        }

        using var message = new MailMessage
        {
            Subject = subject,
            Body = string.IsNullOrWhiteSpace(html) ? text : html,
            IsBodyHtml = !string.IsNullOrWhiteSpace(html),
            From = new MailAddress(fromEmail, string.IsNullOrWhiteSpace(fromName) ? null : fromName)
        };

        message.To.Add(new MailAddress(to));

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = useSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };

        if (useAuth)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return MailSendResult.Failure("SMTP auth is enabled but Smtp:Username or Smtp:Password is missing.");

            client.Credentials = new NetworkCredential(username, password);
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await client.SendMailAsync(message, cancellationToken);
            return MailSendResult.Success("Email sent successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP email send failed");
            return MailSendResult.Failure("SMTP send failed.", ex.Message);
        }
    }
}

public sealed record MailSendResult(bool IsSuccess, string Message, string? ProviderResponse)
{
    public static MailSendResult Success(string message, string? providerResponse = null)
        => new(true, message, providerResponse);

    public static MailSendResult Failure(string message, string? providerResponse = null)
        => new(false, message, providerResponse);
}
