public class ProductCreateDTO
{
    public string? ProductName { get; set; }
    public decimal Price { get; set; }
    public string? ImgUrl { get; set; }
    public int CategoryId { get; set; }
    public string? Description { get; set; }
    public decimal Discount { get; set; }
    // public decimal FinalPrice => Price * (1 - Discount);
    public List<string> GalleryImages { get; set; } = new();
    public int Stock { get; set; }
}