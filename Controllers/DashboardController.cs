using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using productApi.Context;

namespace productApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly productDb _context;

        public DashboardController(productDb context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardData()
        {
            // -----------------------
            // Kullanıcı istatistikleri
            // -----------------------
            var totalUsers = await _context.Users.CountAsync(u => !u.IsDeleted);
            var totalAdmins = await _context.Users.CountAsync(u => u.Role == "Admin" && !u.IsDeleted);
            var totalCustomers = await _context.Users.CountAsync(u => u.Role == "Customer" && !u.IsDeleted);

            var recentUsers = await _context.Users
                .Where(u => !u.IsDeleted)
                .Take(5)
                .Select(u => new { u.Id, u.UserName, u.Email })
                .ToListAsync();

            // -----------------------
            // Sipariş istatistikleri
            // -----------------------
            var totalOrders = await _context.Orders.CountAsync();
            var completedOrders = await _context.Orders.CountAsync(o => o.Status == Order.OrderStatus.Delivered);
            var pendingOrders = await _context.Orders.CountAsync(o => o.Status == Order.OrderStatus.Pending);
            var canceledOrders = await _context.Orders.CountAsync(o => o.Status == Order.OrderStatus.Cancelled);
            var averageOrder = await _context.Orders
                .Where(o => o.Status == Order.OrderStatus.Delivered)
                .AverageAsync(o => o.TotalPrice);

            var recentOrders = await _context.Orders
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .Select(o => new { o.Id, o.UserId, o.TotalPrice, o.Status, o.CreatedAt })
                .ToListAsync();

            // -----------------------
            // Finansal veriler
            // -----------------------
            var totalRevenue = await _context.Orders
                .Where(o => o.Status == Order.OrderStatus.Delivered)
                .SumAsync(o => o.TotalPrice);

            var monthlyRevenue = await _context.Orders
                .Where(o => o.Status == Order.OrderStatus.Delivered)
                .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Total = g.Sum(o => o.TotalPrice) })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            // -----------------------
            // Ürün verileri
            // -----------------------
            var topProducts = await _context.OrderItems
     .Include(oi => oi.Product)
     .GroupBy(oi => new { oi.ProductId, oi.Product!.ProductName })
     .Select(g => new
     {
         ProductId = g.Key.ProductId,
         ProductName = g.Key.ProductName,
         Quantity = g.Sum(oi => oi.Quantity)
     })
     .OrderByDescending(x => x.Quantity)
     .Take(5)
     .ToListAsync();

            // -----------------------
            // Dashboard yanıtı
            // -----------------------
            var dashboardData = new
            {
                userStats = new
                {
                    totalUsers,
                    totalAdmins,
                    totalCustomers,
                    recentUsers
                },
                orderStats = new
                {
                    totalOrders,
                    completedOrders,
                    pendingOrders,
                    canceledOrders,
                    averageOrder,
                    recentOrders
                },
                revenue = new
                {
                    totalRevenue,
                    monthlyRevenue
                },
                topProducts
            };

            return Ok(dashboardData);
        }
    }

}