public class ProductUpdateDTO
{
    public string? ProductName { get; set; }
    public decimal Price { get; set; }
    public string? ImgUrl { get; set; }
    public string? Description { get; set; }
    public decimal Discount { get; set; }
    public int CategoryId { get; set; }
    public List<string> GalleryImages { get; set; } = new();
}