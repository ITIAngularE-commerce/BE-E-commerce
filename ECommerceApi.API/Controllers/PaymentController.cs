using ECommerceApi.Data;
using ECommerceApi.Data.Models;
using ECommerceApi.Services.DTOs.Common;
using ECommerceApi.Services.DTOs.Payment;
using ECommerceApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ECommerceApi.API.Controllers
{
    [ApiController]
    [Route("api/v1/payments")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IPaymobService _paymobService;
        private readonly AppDbContext _db;

        public PaymentController(IPaymentService paymentService, IPaymobService paymobService , AppDbContext db)
        {
            _paymentService = paymentService;
            _paymobService = paymobService;
            _db = db;
        }

        private string GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        }

        [HttpPost("initiate/{orderId}")]
        [Authorize]
        public async Task<IActionResult> InitiatePayment(int orderId)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { success = false, message = "User not authenticated" });
            }

            var result = await _paymentService.InitiatePaymobAsync(orderId, userId);

            if (!result.IsSuccess)
            {
                return BadRequest(new { success = false, message = result.ErrorMessage });
            }

            return Ok(new
            {
                success = true,
                message = result.Message,
                data = new
                {
                    iframeUrl = result.Data!.IframeUrl,
                    paymentToken = result.Data.PaymentToken
                }
            });
        }

        [HttpPost("callback")]
        public async Task<IActionResult> Callback([FromForm] IFormCollection form)
        {
            try
            {
                var orderIdString = form["merchant_order_id"].ToString();
                var success = form["success"].ToString().ToLower() == "true";

                if (int.TryParse(orderIdString, out var orderId))
                {
                    var payment = await _db.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);
                    if (payment != null)
                    {
                        payment.Status = success ? PaymentStatus.Completed : PaymentStatus.Failed;
                        payment.TransactionId = form["id"].ToString();
                        payment.PaidAt = success ? DateTime.UtcNow : null;

                        if (success)
                        {
                            var order = await _db.Orders.FindAsync(orderId);
                            if (order != null && order.Status == OrderStatus.Pending)
                            {
                                order.Status = OrderStatus.Processing;
                            }
                        }
                        await _db.SaveChangesAsync();
                    }
                }

                return Content("SUCCESS", "text/plain");
            }
            catch
            {
                return Content("FAILURE", "text/plain");
            }
        }

        [HttpGet("status/{orderId}")]
        [Authorize]
        public async Task<IActionResult> GetPaymentStatus(int orderId)
        {
            var userId = GetUserId();
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Customer";

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { success = false, message = "User not authenticated" });
            }

            var result = await _paymentService.GetPaymentStatusAsync(orderId, role == "Admin" ? "" : userId);

            if (!result.IsSuccess)
            {
                return NotFound(new { success = false, message = result.ErrorMessage });
            }

            return Ok(new { success = true, data = result.Data });
        }
    }
}