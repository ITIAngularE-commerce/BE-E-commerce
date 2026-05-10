using ECommerceApi.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ECommerceApi.Services.Implementations
{
    public class EmailService(IConfiguration config) : IEmailService
    {
        private SmtpClient BuildClient()
        {
            return new SmtpClient(config["Email:Host"])
            {
                Port = int.Parse(config["Email:Port"]!),
                Credentials = new NetworkCredential(config["Email:Username"], config["Email:Password"]),
                EnableSsl = true
            };
        }

        public async Task SendEmailConfirmationAsync(string toEmail, string userName, string confirmationLink)
        {
            var body = $"""
            <h2>مرحباً {userName}!</h2>
            <p>اضغط على الرابط التالي لتأكيد بريدك الإلكتروني:</p>
            <a href="{confirmationLink}" style="padding:10px 20px;background:#6366F1;color:white;border-radius:5px;text-decoration:none;">
              تأكيد الإيميل
            </a>
            """;

            await SendAsync(toEmail, "تأكيد البريد الإلكتروني", body);
        }

        public async Task SendOrderConfirmationAsync(string toEmail, string userName, int orderId, decimal total)
        {
            var body = $"""
            <h2>شكراً {userName}!</h2>
            <p>تم استلام طلبك رقم <strong>#{orderId}</strong> بنجاح.</p>
            <p>الإجمالي: <strong>{total:C}</strong></p>
            <p>سنقوم بإعلامك عند شحن الطلب.</p>
            """;

            await SendAsync(toEmail, $"تأكيد الطلب #{orderId}", body);
        }

        private async Task SendAsync(string to, string subject, string htmlBody)
        {
            using var msg = new MailMessage
            {
                From = new MailAddress(config["Email:From"]!),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            msg.To.Add(to);
            using var client = BuildClient();
            await client.SendMailAsync(msg);
        }
    }
}
