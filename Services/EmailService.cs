using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace DevToolsSuite.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string message);
        Task SendWelcomeEmailAsync(string toEmail, string userName);
        Task SendPasswordResetEmailAsync(string toEmail, string resetLink);
        Task SendSubscriptionConfirmationAsync(string toEmail, string planName);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            try
            {
                using var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.Port)
                {
                    Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password),
                    EnableSsl = _emailSettings.EnableSsl
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
                    Subject = subject,
                    Body = message,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
                throw;
            }
        }

        public async Task SendWelcomeEmailAsync(string toEmail, string userName)
        {
            var subject = "Welcome to DevTools Suite!";
            var message = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #ff0000, #ff6b6b); color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; background: #f9f9f9; }}
                        .footer {{ padding: 20px; text-align: center; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Welcome to DevTools Suite! 🚀</h1>
                        </div>
                        <div class='content'>
                            <h2>Hello {userName}!</h2>
                            <p>Thank you for joining DevTools Suite - your all-in-one developer toolbox.</p>
                            <ul>
                                <li>✅ JSON Formatter & Validator</li>
                                <li>✅ JWT Decoder</li>
                                <li>✅ Base64 Converter</li>
                                <li>✅ Regex Tester</li>
                                <li>✅ And many more...</li>
                            </ul>
                            <p><a href='https://devtoolssuite.com/tools' style='background: #ff0000; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Explore Tools</a></p>
                        </div>
                        <div class='footer'>
                            <p>Need help? Contact support@devtoolssuite.com</p>
                        </div>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, subject, message);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            var subject = "Reset Your DevTools Suite Password";
            var message = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #ff0000, #ff6b6b); color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; background: #f9f9f9; }}
                        .button {{ background: #ff0000; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Password Reset</h1>
                        </div>
                        <div class='content'>
                            <p>You requested to reset your password for DevTools Suite.</p>
                            <p><a href='{resetLink}' class='button'>Reset Password</a></p>
                            <p>If you didn't request this, please ignore this email.</p>
                            <p><strong>This link will expire in 1 hour.</strong></p>
                        </div>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, subject, message);
        }

        public async Task SendSubscriptionConfirmationAsync(string toEmail, string planName)
        {
            var subject = $"Welcome to {planName} Plan!";
            var message = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #ff0000, #ff6b6b); color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; background: #f9f9f9; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>🎉 Welcome to {planName}!</h1>
                        </div>
                        <div class='content'>
                            <p>Thank you for upgrading to our <strong>{planName}</strong> plan!</p>
                            <ul>
                                <li>✅ Unlimited tool sessions</li>
                                <li>✅ Advanced features</li>
                                <li>✅ Priority support</li>
                                <li>✅ Custom themes</li>
                                <li>✅ Export capabilities</li>
                            </ul>
                            <p><a href='https://devtoolssuite.com/dashboard' style='background: #ff0000; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Go to Dashboard</a></p>
                        </div>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, subject, message);
        }
    }

    public class EmailSettings
    {
        public string SmtpServer { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromEmail { get; set; } = "noreply@devtoolssuite.com";
        public string FromName { get; set; } = "DevTools Suite";
        public bool EnableSsl { get; set; } = true;
    }
}
