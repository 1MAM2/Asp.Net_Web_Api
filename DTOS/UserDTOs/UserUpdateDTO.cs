using System.ComponentModel.DataAnnotations;


public class UserUpdateDTO
{
    [Required]
    public string UserName { get; set; } = string.Empty;
    [Required]
    public string Address { get; set; } = string.Empty;
    [Required]
    public string Email { get; set; } = string.Empty;
}