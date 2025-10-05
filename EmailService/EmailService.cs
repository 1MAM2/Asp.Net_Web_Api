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

    public class EmailService : IEmailService
    {
        private readonly string _sendGridApiKey;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _sendGridApiKey = config["SendGrid:ApiKey"] ?? throw new ArgumentNullException("SendGrid API Key not found");
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string htmlMessage)
        {
            var client = new SendGrid.SendGridClient(_sendGridApiKey);
            var from = new EmailAddress("smetindogan@gmail.com", "EShop.com"); // Gmail adresin
            var toAddress = new EmailAddress(to);
            var msg = MailHelper.CreateSingleEmail(from, toAddress, subject, null, htmlMessage);

            var response = await client.SendEmailAsync(msg);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Body.ReadAsStringAsync();
                _logger.LogError("SendGrid error: {Body}", body);
            }
        }
    }
}