using static Order;

public class OrderReadDTO
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal TotalPrice { get; set; }

    public List<OrderItemReadDTO>? OrderItems { get; set; }
    public OrderStatus Status { get; set; }

}