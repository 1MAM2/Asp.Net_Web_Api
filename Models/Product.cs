using System.ComponentModel.DataAnnotations.Schema;

public class Product
{
    public int Id { get; set; }
    public string? ProductName { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }
    public string? Description  { get; set; }
    public decimal Discount { get; set; } = 0;
    public string? ImgUrl { get; set; }
    public int CategoryId { get; set; }
    public ICollection<ProductImage> GalleryImages { get; set; } = new List<ProductImage>();
    public bool IsDeleted { get; set; }
    public Category? Category { get; set; }

    [NotMapped]
    public decimal FinalPrice => Price * (1 - Discount);
}