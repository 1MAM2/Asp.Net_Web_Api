using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace productApi.DTOS.UserDTOs
{
    public class TokenResponseDTO
    {
        public required string AccessToken { get; set; }
        public required string RefreshToken { get; set; }
        public int UserId { get; set; }  
    }
}