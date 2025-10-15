



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
    [HttpGet]
    public async Task<ActionResult<List<OrderReadDTO>>> GetAllOrders()
    {
        var orders = await _context.Orders
        .Include(i => i.OrderItems)
        .ToListAsync();
        var ordersDto = orders.Select(order => new OrderReadDTO
        {
            OrderId = order.Id,
            UserId = order.UserId,
            CreatedAt = order.CreatedAt,
            TotalPrice = order.TotalPrice,
            OrderItems = order.OrderItems.Select(oi => new OrderItemReadDTO
            {
                ProductId = oi.ProductId,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                TotalPrice = oi.TotalPrice,
                ImgUrl = oi.ImgUrl,
                ProductName = oi.ProductName
            }).ToList(),
            Status = order.Status,
        });

        return Ok(value: ordersDto);
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
                Status = o.Status,
                OrderItems = o.OrderItems.Select(oi => new OrderItemReadDTO
                {
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    TotalPrice = oi.Quantity * oi.UnitPrice,
                    ImgUrl = oi.ImgUrl,
                    ProductName = oi.ProductName
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
                UnitPrice = oi.UnitPrice,
                ImgUrl = oi.ImgUrl,
                ProductName = oi.ProductName
            }).ToList()
        };
        foreach (var item in order.OrderItems)
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            if (product == null)
                return BadRequest($"Product with ID {item.ProductId} not found.");

            if (product.Stock < item.Quantity)
                return BadRequest($"Not enough stock for product: {product.ProductName}");

            product.Stock -= item.Quantity; // stok düş
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var orderReadDTO = new OrderReadDTO
        {
            UserId = order.UserId,
            CreatedAt = order.CreatedAt,
            TotalPrice = order.TotalPrice,
            Status = order.Status,
            OrderItems = order.OrderItems.Select(oi => new OrderItemReadDTO
            {
                ProductId = oi.ProductId,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                TotalPrice = oi.Quantity * oi.UnitPrice,
                ImgUrl = oi.ImgUrl,
                ProductName = oi.ProductName
            }).ToList()
        };

        return new JsonResult(new
        {
            OrderId = order.Id,
            order.TotalPrice,
            order.Status,
            order.UserId,
            order.CreatedAt
        });

    }
    [HttpGet("{id}")]
    public async Task<ActionResult<OrderReadDTO>> GetOrderById(int id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        var orderReadDTO = new OrderReadDTO
        {
            UserId = order.UserId,
            CreatedAt = order.CreatedAt,
            TotalPrice = order.TotalPrice,
            Status = order.Status,
            OrderItems = order.OrderItems.Select(oi => new OrderItemReadDTO
            {
                ProductId = oi.ProductId,
                ProductName = oi.Product!.ProductName,
                ImgUrl = oi.Product.ImgUrl,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                TotalPrice = oi.TotalPrice
            }).ToList(),
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
        order.CreatedAt = DateTime.Now;
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