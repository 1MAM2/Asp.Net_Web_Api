using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace productApi.DTOS.UserDTOs
{
    public class RefreshTokenRequestDTO
    {
        public int UserId { get; set; }
        public required string refreshToken { get; set; }
    }
}