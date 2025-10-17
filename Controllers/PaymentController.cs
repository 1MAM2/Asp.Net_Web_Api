using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using productApi.Context;
using productApi.Hubs;
using System.Globalization;
using Newtonsoft.Json;

namespace productApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly ILogger<PaymentController> _logger;
        private readonly IHubContext<PayHub> _hubContext;
        private readonly productDb _context;

        public PaymentController(ILogger<PaymentController> logger, IHubContext<PayHub> hubContext, productDb context)
        {
            _logger = logger;
            _hubContext = hubContext;
            _context = context;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [HttpPost("pay/{transactionId}")]
        public async Task<IActionResult> Pay([FromRoute] string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId))
                return BadRequest("TransactionId is required");

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id.ToString() == transactionId);

            if (order == null || order.User == null)
                return NotFound("Order or User not found");

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            Options options = new Options
            {
                ApiKey = "sandbox-bICJ3E6VTMfSXNFHUpBCXlbNCumDgxBc",
                SecretKey = "sandbox-KHh12VSH2onJaWsEOflMJbMwXsZkIUXs",
                BaseUrl = "https://sandbox-api.iyzipay.com"
            };

            // Payment request
            var request = new CreatePaymentRequest
            {
                Locale = Locale.TR.ToString(),
                ConversationId = order.Id.ToString(),
                Price = order.TotalPrice.ToString("0.00", CultureInfo.InvariantCulture),
                PaidPrice = order.TotalPrice.ToString("0.00", CultureInfo.InvariantCulture),
                Currency = Currency.TRY.ToString(),
                Installment = 1,
                PaymentChannel = PaymentChannel.WEB.ToString(),
                PaymentGroup = PaymentGroup.PRODUCT.ToString(),
                CallbackUrl = "https://asp-net-web-api-ym61.onrender.com/api/Payment/pay-callback",
                PaymentCard = new PaymentCard
                {
                    CardHolderName = "John Doe",
                    CardNumber = "5528790000000008",
                    ExpireMonth = "12",
                    ExpireYear = "2030",
                    Cvc = "123",
                    RegisterCard = 0
                },
                Buyer = new Buyer
                {
                    Id = order.User.Id.ToString(),
                    Name = order.User.UserName,
                    Surname = "Surname",
                    GsmNumber = "",
                    Email = order.User.Email,
                    IdentityNumber = "11111111111",
                    RegistrationAddress = order.User.Address,
                    Ip = ipAddress,
                    City = "Istanbul",
                    Country = "Turkey",
                    ZipCode = "34732"
                },
                ShippingAddress = new Address
                {
                    ContactName = order.User.UserName,
                    City = "Istanbul",
                    Country = "Turkey",
                    Description = order.User.Address,
                    ZipCode = "34732"
                },
                BillingAddress = new Address
                {
                    ContactName = order.User.UserName,
                    City = "Istanbul",
                    Country = "Turkey",
                    Description = order.User.Address,
                    ZipCode = "34732"
                },
                BasketItems = order.OrderItems.Select((item, index) => new BasketItem
                {
                    Id = "BI" + (index + 1),
                    Name = item.Product?.ProductName ?? "Undefined",
                    Category1 = "Default",
                    Category2 = "General",
                    ItemType = BasketItemType.PHYSICAL.ToString(),
                    Price = (item.UnitPrice * item.Quantity).ToString("F2", CultureInfo.InvariantCulture)
                }).ToList()
            };

            try
            {
                ThreedsInitialize threedsInitialize = await ThreedsInitialize.Create(request, options);
                return Ok(new { Content = threedsInitialize.HtmlContent, ConversationId = request.ConversationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment error");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("pay-callback")]
        public async Task<IActionResult> PayCallBack([FromForm] IFormCollection collections)
        {
            var data = new CallBackData(
                Status: collections["status"],
                PaymentId: collections["paymentId"]!,
                ConversationData: collections["conversationData"],
                ConversationId: collections["conversationId"]!,
                MDStatus: collections["mdStatus"]
            );

            if (data.Status != "success")
                return BadRequest("Payment failed!");

            var orderId = int.Parse(data.ConversationId);
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.Status = Order.OrderStatus.Paid;
                order.PaymentDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            if (PayHub.TransactionConnections.TryGetValue(data.ConversationId, out var connectionId))
            {
                await _hubContext.Clients.Client(connectionId).SendAsync("Receive", data);
            }

            return Ok();
        }

        public sealed record CallBackData(string? Status, string PaymentId, string? ConversationData, string ConversationId, string? MDStatus);
    }
}