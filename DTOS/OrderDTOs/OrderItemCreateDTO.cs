public class OrderItemCreateDTO
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? ImgUrl { get; set; }
    public string? ProductName { get; set; }

}