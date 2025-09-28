using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace productApi.DTOS.UserDTOs
{
    public class TokenResponseDTO
    {
        public required string accessToken { get; set; }
        public required string refreshToken { get; set; }
        public int UserId { get; set; }  
    }
}