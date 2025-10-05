using SendGrid;
using SendGrid.Helpers.Mail;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlMessage)
    {
        var apiKey = _config["SENDGRID_API_KEY"];
        var client = new SendGridClient(apiKey);

        var from = new EmailAddress(_config["SENDGRID_FROM_EMAIL"], _config["SENDGRID_FROM_NAME"]);
        var toEmail = new EmailAddress(to);
        var msg = MailHelper.CreateSingleEmail(from, toEmail, subject, plainTextContent: null, htmlContent: htmlMessage);

        try
        {
            var response = await client.SendEmailAsync(msg);
            if (response.StatusCode != System.Net.HttpStatusCode.Accepted &&
                response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                var body = await response.Body.ReadAsStringAsync();
                _logger.LogError("SendGrid error: {StatusCode} - {Body}", response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendGrid exception: {Message}", ex.Message);
            throw;
        }
    }
}