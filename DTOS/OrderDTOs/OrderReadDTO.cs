public class OrderReadDTO
{
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal TotalPrice { get; set; }

    public List<OrderItemReadDTO>? OrderItems { get; set; }
    public string? Status { get; set; }

}