using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using productApi.Context;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Customer,Admin")]
public class OrderController : ControllerBase
{
    private readonly productDb _context;

    public OrderController(productDb context)
    {
        _context = context;
    }

    // GET: api/order
    [HttpGet]
    public async Task<ActionResult<List<OrderReadDTO>>> GetAllOrders()
    {
        var ordersDto = await _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .Select(order => new OrderReadDTO
            {
                Id = order.Id,
                UserId = order.UserId,
                CreatedAt = order.CreatedAt,
                TotalPrice = order.TotalPrice,
                Status = order.Status,
                OrderItems = order.OrderItems.Select(oi => new OrderItemReadDTO
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.ProductName,
                    ImgUrl = oi.ImgUrl,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    TotalPrice = oi.TotalPrice
                }).ToList()
            })
            .ToListAsync();

        return Ok(ordersDto);
    }

    // GET: api/order/user-getall-orders
    [HttpGet("user-getall-orders")]
    public async Task<ActionResult<List<OrderReadDTO>>> GetOrdersForUser()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        if (userId == 0) return Unauthorized("User not found");

        var orders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .Include(o => o.OrderItems)
            .Select(o => new OrderReadDTO
            {
                UserId = o.UserId,
                CreatedAt = o.CreatedAt,
                TotalPrice = o.TotalPrice,
                Status = o.Status,
                OrderItems = o.OrderItems.Select(oi => new OrderItemReadDTO
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.ProductName,
                    ImgUrl = oi.ImgUrl,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    TotalPrice = oi.Quantity * oi.UnitPrice
                }).ToList()
            })
            .ToListAsync();

        return Ok(orders);
    }

    // POST: api/order/create-order
    [HttpPost("create-order")]
    public async Task<ActionResult<OrderReadDTO>> CreateOrder(OrderCreateDTO orderDTO)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        if (userId == 0) return Unauthorized("User not found");

        var orderItems = new List<OrderItem>();
        decimal totalPrice = 0;

        foreach (var itemDto in orderDTO.OrderItems!)
        {
            var product = await _context.Products.FindAsync(itemDto.ProductId);
            if (product == null) return BadRequest($"Product with ID {itemDto.ProductId} not found.");
            if (product.Stock < itemDto.Quantity)
                return BadRequest($"Not enough stock for product: {product.ProductName}");

            product.Stock -= itemDto.Quantity;

            orderItems.Add(new OrderItem
            {
                ProductId = itemDto.ProductId,
                ProductName = itemDto.ProductName,
                ImgUrl = itemDto.ImgUrl,
                Quantity = itemDto.Quantity,
                UnitPrice = itemDto.UnitPrice,
                TotalPrice = itemDto.Quantity * itemDto.UnitPrice
            });

            totalPrice += itemDto.Quantity * itemDto.UnitPrice;
        }

        var order = new Order
        {
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            Status = Order.OrderStatus.Pending,
            TotalPrice = totalPrice,
            OrderItems = orderItems
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            order.Id,
            order.UserId,
            order.TotalPrice,
            order.Status,
            order.CreatedAt
        });
    }

    // GET: api/order/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<OrderReadDTO>> GetOrderById(int id)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        return Ok(new OrderReadDTO
        {
            UserId = order.UserId,
            CreatedAt = order.CreatedAt,
            TotalPrice = order.TotalPrice,
            Status = order.Status,
            OrderItems = order.OrderItems.Select(oi => new OrderItemReadDTO
            {
                ProductId = oi.ProductId,
                ProductName = oi.ProductName,
                ImgUrl = oi.ImgUrl,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                TotalPrice = oi.TotalPrice
            }).ToList()
        });
    }

    // PATCH: api/order/{id}/status
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int id, string status)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound();

        if (!Enum.TryParse<Order.OrderStatus>(status, true, out var newStatus))
            return BadRequest("Invalid status value");

        order.Status = newStatus;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/order/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound();

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
