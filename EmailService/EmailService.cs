
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;


public class EmailService : IEmailService
{

    private readonly SmtpSettings _smtp;
    private readonly ILogger<EmailService> _logger;
    public EmailService(IOptions<SmtpSettings> smtpOptions, ILogger<EmailService> logger)
    {
        _smtp = smtpOptions.Value;
        _logger = logger;
    }
    public async Task SendEmailAsync(string to, string subject, string htmlMessage)
    {
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(_smtp.FromName ?? "", _smtp.FromEmail));
        email.To.Add(MailboxAddress.Parse(to));
        email.Subject = subject;

        var builder = new BodyBuilder { HtmlBody = htmlMessage };
        email.Body = builder.ToMessageBody();

        using var client = new SmtpClient();

        try
        {
            var secureOption = _smtp.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
            await client.ConnectAsync(_smtp.Host, _smtp.Port, secureOption);

            if (!string.IsNullOrWhiteSpace(_smtp.User))
            {
                await client.AuthenticateAsync(_smtp.User, _smtp.Password);
            }
            await client.SendAsync(email);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error accured : {Message}", ex.Message);
            throw;
        }
    }
}