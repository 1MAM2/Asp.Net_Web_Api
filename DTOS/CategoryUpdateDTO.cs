using System.ComponentModel.DataAnnotations;

public class CategoryUpdateDTO
{
    [Required]
    public string? CategoryName { get; set; }
}