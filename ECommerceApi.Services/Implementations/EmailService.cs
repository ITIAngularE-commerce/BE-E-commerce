using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Net.Mail;

namespace ECommerceApi.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        private async Task<ApiResponse<bool>> SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
                message.To.Add(new MailboxAddress(toName, toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = htmlBody
                };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new MailKit.Net.Smtp.SmtpClient();
                await client.ConnectAsync(_settings.SmtpServer, _settings.SmtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_settings.SenderEmail, _settings.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {Email}", toEmail);
                return ApiResponse<bool>.Success(true, "Email sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
                return ApiResponse<bool>.Failure($"Failed to send email: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> SendEmailConfirmationAsync(string email, string fullName, string confirmationLink)
        {
            var htmlBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Welcome to E-Commerce Store! 🎉</h2>
                    <p>Hello <strong>{fullName}</strong>,</p>
                    <p>Thank you for registering! Please confirm your email address by clicking the button below:</p>
                    <a href='{confirmationLink}' style='background-color: #4CAF50; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; display: inline-block;'>Confirm Email</a>
                    <p>Or copy this link: <a href='{confirmationLink}'>{confirmationLink}</a></p>
                    <p>If you didn't create an account, please ignore this email.</p>
                    <br>
                    <p>Best regards,<br>E-Commerce Store Team</p>
                </body>
                </html>";

            return await SendEmailAsync(email, fullName, "Confirm Your Email", htmlBody);
        }

        public async Task<ApiResponse<bool>> SendWelcomeEmailAsync(string email, string fullName)
        {
            var htmlBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Welcome to E-Commerce Store! 🎊</h2>
                    <p>Hello <strong>{fullName}</strong>,</p>
                    <p>Your email has been confirmed successfully!</p>
                    <p>You can now start shopping and enjoy our exclusive offers.</p>
                    <a href='https://yourstore.com' style='background-color: #4CAF50; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; display: inline-block;'>Start Shopping</a>
                    <br><br>
                    <p>Best regards,<br>E-Commerce Store Team</p>
                </body>
                </html>";

            return await SendEmailAsync(email, fullName, "Welcome to Our Store!", htmlBody);
        }

        public async Task<ApiResponse<bool>> SendOrderConfirmationAsync(string email, string fullName, int orderId, decimal total)
        {
            var htmlBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Order Confirmed! 🛍️</h2>
                    <p>Hello <strong>{fullName}</strong>,</p>
                    <p>Thank you for your order! We have received your order successfully.</p>
                    <div style='background-color: #f5f5f5; padding: 15px; border-radius: 8px;'>
                        <p><strong>Order ID:</strong> #{orderId}</p>
                        <p><strong>Total Amount:</strong> ${total:F2}</p>
                        <p><strong>Status:</strong> Pending</p>
                    </div>
                    <p>We will notify you once your order is processed.</p>
                    <a href='https://yourstore.com/orders/{orderId}' style='background-color: #2196F3; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; display: inline-block;'>Track Order</a>
                    <br><br>
                    <p>Best regards,<br>E-Commerce Store Team</p>
                </body>
                </html>";

            return await SendEmailAsync(email, fullName, $"Order Confirmation #{orderId}", htmlBody);
        }

        public async Task<ApiResponse<bool>> SendOrderStatusUpdateAsync(string email, string fullName, int orderId, string oldStatus, string newStatus)
        {
            var statusColors = new Dictionary<string, string>
            {
                { "Pending", "#FF9800" },
                { "Processing", "#2196F3" },
                { "Shipped", "#9C27B0" },
                { "Delivered", "#4CAF50" },
                { "Cancelled", "#F44336" }
            };

            var color = statusColors.ContainsKey(newStatus) ? statusColors[newStatus] : "#333333";

            var htmlBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Order Status Update 📦</h2>
                    <p>Hello <strong>{fullName}</strong>,</p>
                    <p>Your order status has been updated:</p>
                    <div style='background-color: #f5f5f5; padding: 15px; border-radius: 8px;'>
                        <p><strong>Order ID:</strong> #{orderId}</p>
                        <p><strong>Old Status:</strong> {oldStatus}</p>
                        <p><strong>New Status:</strong> <span style='color: {color}; font-weight: bold;'>{newStatus}</span></p>
                    </div>";

            if (newStatus == "Shipped")
            {
                htmlBody += @"<p>🎉 Good news! Your order has been shipped and is on its way to you.</p>";
            }
            else if (newStatus == "Delivered")
            {
                htmlBody += @"<p>✅ Your order has been delivered! We hope you enjoy your purchase.</p>";
            }
            else if (newStatus == "Cancelled")
            {
                htmlBody += @"<p>⚠️ Your order has been cancelled. If you have any questions, please contact support.</p>";
            }

            htmlBody += @"
                    <a href='https://yourstore.com/orders/{orderId}' style='background-color: #2196F3; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; display: inline-block;'>Track Order</a>
                    <br><br>
                    <p>Best regards,<br>E-Commerce Store Team</p>
                </body>
                </html>";

            return await SendEmailAsync(email, fullName, $"Order #{orderId} Status Update - {newStatus}", htmlBody);
        }

        public async Task<ApiResponse<bool>> SendPaymentConfirmationAsync(string email, string fullName, int orderId, decimal amount)
        {
            var htmlBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Payment Confirmed 💳</h2>
                    <p>Hello <strong>{fullName}</strong>,</p>
                    <p>Your payment has been confirmed successfully!</p>
                    <div style='background-color: #f5f5f5; padding: 15px; border-radius: 8px;'>
                        <p><strong>Order ID:</strong> #{orderId}</p>
                        <p><strong>Amount Paid:</strong> ${amount:F2}</p>
                    </div>
                    <p>We are now processing your order.</p>
                    <br>
                    <p>Best regards,<br>E-Commerce Store Team</p>
                </body>
                </html>";

            return await SendEmailAsync(email, fullName, $"Payment Confirmed for Order #{orderId}", htmlBody);
        }

        public async Task<ApiResponse<bool>> SendPasswordResetAsync(string email, string fullName, string resetLink)
        {
            var htmlBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Password Reset Request 🔐</h2>
                    <p>Hello <strong>{fullName}</strong>,</p>
                    <p>We received a request to reset your password. Click the button below to create a new password:</p>
                    <a href='{resetLink}' style='background-color: #FF9800; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; display: inline-block;'>Reset Password</a>
                    <p>If you didn't request this, please ignore this email.</p>
                    <p>This link will expire in 1 hour.</p>
                    <br>
                    <p>Best regards,<br>E-Commerce Store Team</p>
                </body>
                </html>";

            return await SendEmailAsync(email, fullName, "Reset Your Password", htmlBody);
        }
    }
}