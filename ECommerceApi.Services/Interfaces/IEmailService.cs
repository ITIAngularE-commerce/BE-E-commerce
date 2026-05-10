using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerceApi.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailConfirmationAsync(string toEmail, string userName, string confirmationLink);
        Task SendOrderConfirmationAsync(string toEmail, string userName, int orderId, decimal total);
    }
}
