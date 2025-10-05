// using SendGrid;
// using SendGrid.Helpers.Mail;

// public class EmailService : IEmailService
// {
//     private readonly string _sendGridApiKey;
//     private readonly ILogger<EmailService> _logger;

//     public EmailService(IConfiguration config, ILogger<EmailService> logger)
//     {
//         _sendGridApiKey = config["SendGrid:ApiKey"] 
//             ?? throw new ArgumentNullException("SendGrid API Key bulunamadı");
//         _logger = logger;
//     }

//     public async Task SendEmailAsync(string to, string subject, string htmlMessage)
// {
//     var client = new SendGridClient(_sendGridApiKey);

//     // Buradaki from adresi Single Sender olarak doğrulanmış olmalı
//     var from = new EmailAddress("smetindogan@gmail.com", "EShop.com");
//     var toAddress = new EmailAddress(to);

//     var msg = MailHelper.CreateSingleEmail(from, toAddress, subject, null, htmlMessage);
//     var response = await client.SendEmailAsync(msg);

//     if (!response.IsSuccessStatusCode)
//     {
//         var body = await response.Body.ReadAsStringAsync();
//         _logger.LogError("SendGrid error: {Body}", body);
//     }
// }
// }