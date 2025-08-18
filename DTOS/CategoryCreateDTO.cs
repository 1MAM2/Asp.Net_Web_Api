using System.ComponentModel.DataAnnotations;

public class CategoryCreateDTO
{
    [Required]
    public string? CategoryName { get; set; }
}