using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace productApi.Models
{
    public class User
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "Customer";
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        // yerelleştirme
        public string? PhoneNumber { get; set; }
        public bool IsDeleted { get; set; } = false;
        public bool IsEmailConfirmed { get; set; } = false;
        public string? EmailConfirmationToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
    }
}