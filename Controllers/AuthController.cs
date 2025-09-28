using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualBasic;
using productApi.Context;
using productApi.DTOS.UserDTOs;
using productApi.Models;
using Microsoft.AspNetCore.Authorization;

namespace productApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly productDb _context;
        private readonly IConfiguration _config;
        public AuthController(productDb context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }
        [HttpPost("register")]
        public async Task<ActionResult<User>> RegisterAsync(UserRegisterDTO request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == request.UserName);
            if (user != null)
            {
                return BadRequest("You have already an account");
            }
            var hashedPassword = new PasswordHasher<User>();
            var newUser = new User();

            newUser.UserName = request.UserName;
            newUser.PasswordHash = hashedPassword.HashPassword(newUser, request.Password);
            newUser.Role = string.IsNullOrWhiteSpace(request.Role) ? "Customer" : request.Role;
            newUser.Address = request.Address;
            newUser.Email = request.Email;

            _context.Users.Add(newUser);

            await _context.SaveChangesAsync();

            return Ok(new { newUser.UserName });
        }
        [HttpPost("login")]
        public async Task<ActionResult<TokenResponseDTO>> LoginAsync(UserDTO request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == request.UserName);
            if (user == null)
            {
                return BadRequest("Account not found");
            }

            if (new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            {
                return Unauthorized("Username or password wrong");
            }
            TokenResponseDTO response = await CreateTokenResponse(user);

            return response;
        }
        [HttpPost("logout")]
        public async Task<ActionResult> LogoutAsync([FromQuery] int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = DateTime.MinValue;

            await _context.SaveChangesAsync();
            return Ok("Logout succes");
        }
        [HttpPost("refresh-token")]
        public async Task<ActionResult<TokenResponseDTO>> RefreshTokenAsync(RefreshTokenRequestDTO request)
        {
            if (string.IsNullOrEmpty(request.refreshToken))
            {
                return Unauthorized("Invalid refresh token");
            }


            var user = await ValidateRefreshTokenAsync(request.UserId, request.refreshToken);
            if (user == null)
            {
                return Unauthorized("Invalid or expired refresh token");
            }
            return await CreateTokenResponse(user);
        }
        [Authorize]
        [HttpGet("test")]
        public IActionResult TestSecureEndpoint()
        {
            return Ok("Burası güvenli bir endpoint!");
        }
        private async Task<TokenResponseDTO> CreateTokenResponse(User user)
        {
            return new TokenResponseDTO
            {
                accessToken = CreateToken(user),
                refreshToken = await GenerateAndSaveRefreshTokenAsync(user),
                UserId = user.Id,
            };

        }
        private string CreateToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName ),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role,user.Role),
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.GetValue<string>("AppSettings:Token")!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

            var tokenDescriptor = new JwtSecurityToken(
                issuer: _config.GetValue<string>("AppSettings:Issuer"),
                audience: _config.GetValue<string>("AppSettings:Audience"),
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private async Task<string> GenerateAndSaveRefreshTokenAsync(User user)
        {
            var refreshToken = GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();
            return refreshToken;
        }


        private async Task<User?> ValidateRefreshTokenAsync(int userId, string refreshToken)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return null;
            }
            return user;
        }
    }
}