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
        public async Task<IActionResult> Pay([FromRoute] PaymentRequestDTO req)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            try
            {
                var order = _context.Orders.FirstOrDefault(o => o.Id.ToString() == req.TransactionId);
                if (order == null) return NotFound("Order not found");
                Options options = new Options()
                {
                    ApiKey = "sandbox-bICJ3E6VTMfSXNFHUpBCXlbNCumDgxBc",
                    SecretKey = "sandbox-KHh12VSH2onJaWsEOflMJbMwXsZkIUXs",
                    BaseUrl = "https://sandbox-api.iyzipay.com",
                };

                CreatePaymentRequest request = new CreatePaymentRequest
                {
                    Locale = Locale.TR.ToString(),
                    ConversationId = order.Id.ToString(),
                    Price = order.TotalPrice.ToString("0.00", CultureInfo.InvariantCulture),
                    PaidPrice = order.TotalPrice.ToString("0.00", CultureInfo.InvariantCulture),
                    Currency = Currency.TRY.ToString(),
                    Installment = 1,
                    PaymentChannel = PaymentChannel.WEB.ToString(),
                    PaymentGroup = PaymentGroup.PRODUCT.ToString(),
                    CallbackUrl = "https://asp-net-web-api-ym61.onrender.com/api/Payment/pay-callback"
                };
                PaymentCard paymentCard = new PaymentCard();
                paymentCard.CardHolderName = "John Doe";
                paymentCard.CardNumber = "5528790000000008";
                paymentCard.ExpireMonth = "12";
                paymentCard.ExpireYear = "2030";
                paymentCard.Cvc = "123";
                paymentCard.RegisterCard = 0;
                request.PaymentCard = paymentCard;

                var userId = _context.Orders
                .Include(o => o.User)
                .FirstOrDefault();
                if (order == null || order.User == null)
                    throw new Exception("Order veya kullanıcı bulunamadı");

                var user = order.User;
                Buyer buyer = new Buyer();
                buyer.Id = user.Id.ToString();
                buyer.Name = user.UserName;
                buyer.Surname = "Surname";
                buyer.GsmNumber = "";
                buyer.Email = user.Email;
                buyer.IdentityNumber = "11111111111";
                buyer.RegistrationAddress = user.Address;
                buyer.Ip = ipAddress;
                buyer.City = "Istanbul";
                buyer.Country = "Turkey";
                buyer.ZipCode = "34732";
                request.Buyer = buyer;

                Address shippingAddress = new Address();
                shippingAddress.ContactName = user.UserName;
                shippingAddress.City = "İstanbul";
                // burası default dikkat et
                shippingAddress.Country = "Turkey";
                shippingAddress.Description = user.Address;
                shippingAddress.ZipCode = "İstanbul";
                request.ShippingAddress = shippingAddress;

                Address billingAddress = new Address();
                billingAddress.ContactName = user.UserName;
                billingAddress.City = "İstanbul";
                //// burası default dikkat et
                billingAddress.Country = "Turkey";
                billingAddress.Description = user.Address;
                billingAddress.ZipCode = "34762";
                request.BillingAddress = billingAddress;

                var orderWithItems = await _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product) // Product bilgilerini de yükle
                    .FirstOrDefaultAsync(o => o.Id.ToString() == req.TransactionId);


                var basketItems = orderWithItems!.OrderItems.Select((item, index) => new BasketItem
                {
                    Id = "BI" + (index + 1), // Her item için benzersiz ID
                    Name = item.Product?.ProductName ?? "Undefined", // Ürün adı
                    Category1 = "Default", // Ürün kategorisi (opsiyonel)
                    Category2 = "General", // İkinci kategori (opsiyonel)
                    ItemType = BasketItemType.PHYSICAL.ToString(), // Ürün tipi, PHYSICAL veya VIRTUAL
                    Price = (item.UnitPrice * item.Quantity).ToString("F2", System.Globalization.CultureInfo.InvariantCulture)

                }).ToList();
                request.BasketItems = basketItems;

                // Payment payment = await Payment.Create(request, options); 3d siz ödeme başlatıyor
                ThreedsInitialize threedsInitialize = await ThreedsInitialize.Create(request, options);
                return Ok(new { Content = threedsInitialize.HtmlContent, ConversationId = request.ConversationId });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Hata mesajı:" + ex);
            }
            return Ok();
        }
        [HttpPost("pay-callback")]
        public async Task<IActionResult> PayCallBack([FromForm] IFormCollection collections)
        {

            CallBackData data = new(
                Status: collections["status"],
                PaymentId: collections["paymentId"]!,
                ConversationData: collections["conversationData"],
                ConversationId: collections["conversationId"]!,
                MDStatus: collections["mdStatus"]
            );
            if (data.Status != "success")
            {
                return BadRequest("Payment faild!");
            }
            var order = await _context.Orders
     .FirstOrDefaultAsync(o => o.Id == int.Parse(data.ConversationId));
            if (order != null)
            {
                order.Status = Order.OrderStatus.Paid;
                order.PaymentDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            // ödemeyi isteyen kişiye msj atıyoruz
            await _hubContext.Clients.Client(PayHub.TransactionConnections[data.ConversationId]).SendAsync("Receive", data);

            return Ok();
        }
        public sealed record CallBackData(string? Status, string PaymentId, string? ConversationData, string ConversationId, string? MDStatus);

    }
}