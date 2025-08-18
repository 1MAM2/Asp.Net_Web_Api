public class ProductReadDTO
{
    public int Id { get; set; }
    public string ProductName { get; set; }
    public decimal Price { get; set; }
    public decimal Discount { get; set; }
    public string? Description { get; set; }
    public decimal FinalPrice { get; set; }
    public string ImgUrl { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; }
    public List<string> GalleryImages { get; set; } = new();
}