


using System.ComponentModel.DataAnnotations;

public class UserReadDTO
{
    public int Id { get; set; }
    [Required]
    public string UserName { get; set; } = string.Empty;
    [Required]
    public string Address { get; set; } = string.Empty;
    [Required]
    public string Email { get; set; } = string.Empty;
    [Required]
    public string Role { get; set; } = "Customer";
    public string? PhoneNumber { get; set; }
}