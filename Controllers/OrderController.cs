



using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using productApi.Context;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Customer")]
public class OrderController : ControllerBase
{
    private readonly productDb _context;

    public OrderController(productDb context)
    {
        _context = context;
    }
    [HttpGet("user-getall-orders")]
    public async Task<ActionResult<List<OrderReadDTO>>> GetOrderAsync()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdStr != null)
        {
            var userId = int.Parse(userIdStr.Value);
            var orders = await _context.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.OrderItems)
            .ToListAsync();

            var sendOrderReadDTOs = orders.Select(o => new OrderReadDTO
            {
                UserId = o.UserId,
                CreatedAt = o.CreatedAt,
                TotalPrice = o.TotalPrice,
                Status = o.Status.ToString(),
                OrderItems = o.OrderItems.Select(oi => new OrderItemReadDTO
                {
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    TotalPrice = oi.Quantity * oi.UnitPrice
                }).ToList(),
            }).ToList();
            return Ok(sendOrderReadDTOs);
        }
        return NotFound("User not found or id broken");
    }

    [HttpPost("create-order")]
    public async Task<ActionResult<OrderReadDTO>> CreateOrder(OrderCreateDTO orderDTO)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized("User not found");

        var userId = int.Parse(userIdClaim.Value);

        var order = new Order
        {
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            Status = Order.OrderStatus.Pending,
            TotalPrice = orderDTO.OrderItems!.Sum(i => i.Quantity * i.UnitPrice),
            OrderItems = orderDTO.OrderItems!.Select(oi => new OrderItem
            {
                ProductId = oi.ProductId,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice
            }).ToList()
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // DTO ile geri döndür
        var orderReadDTO = new OrderReadDTO
        {
            UserId = order.UserId,
            CreatedAt = order.CreatedAt,
            TotalPrice = order.TotalPrice,
            Status = order.Status.ToString(),
            OrderItems = order.OrderItems.Select(oi => new OrderItemReadDTO
            {
                ProductId = oi.ProductId,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                TotalPrice = oi.Quantity * oi.UnitPrice
            }).ToList()
        };

        return Ok(orderReadDTO);
    }
    [HttpGet("{id}")]
    public async Task<ActionResult<OrderReadDTO>> GetOrderById(int id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        var orderReadDTO = new OrderReadDTO
        {
            UserId = order.UserId,
            CreatedAt = order.CreatedAt,
            TotalPrice = order.TotalPrice,
            Status = order.Status.ToString(),
            OrderItems = order.OrderItems.Select(oi => new OrderItemReadDTO
            {
                ProductId = oi.ProductId,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                TotalPrice = oi.Quantity * oi.UnitPrice
            }).ToList()
        };

        return Ok(orderReadDTO);
    }
    [HttpPatch("{id}/status")]
    public async Task<ActionResult> UpdateOrderStatus(int id, string status)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound();

        if (!Enum.TryParse<Order.OrderStatus>(status, out var newStatus))
            return BadRequest("Invalid status value");

        order.Status = newStatus;
        await _context.SaveChangesAsync();

        return NoContent();
    }
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteOrder(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound();

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}