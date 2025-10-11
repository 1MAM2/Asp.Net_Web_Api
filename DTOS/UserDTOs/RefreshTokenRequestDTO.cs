using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace productApi.DTOS.UserDTOs
{
    public class RefreshTokenRequestDTO
    {
        public required string refreshtoken { get; set; }
    }
}