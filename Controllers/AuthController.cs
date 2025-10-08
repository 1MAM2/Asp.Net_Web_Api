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
using Resend;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace productApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly productDb _context;
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _config;
        // private readonly IEmailService _emailService;
        public AuthController(productDb context, IConfiguration config, ILogger<AuthController> logger)
        {
            _context = context;
            _config = config;
            _logger = logger;
        }
        [HttpPost("register")]
        public async Task<ActionResult<User>> RegisterAsync(UserRegisterDTO request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return BadRequest("Email already exists");
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
            newUser.EmailConfirmationToken = Guid.NewGuid().ToString();

            _context.Users.Add(newUser);

            await _context.SaveChangesAsync();

            await SendVeryMailAsync(newUser);

            return Ok(new { newUser.UserName });
        }

        [HttpGet("sendmail")]
        public async Task SendVeryMailAsync(User user)
        {
            IResend resend = ResendClient.Create("re_3gKy9BkJ_Defxc4aNFVKeuGiCBh4A2SNF");
            var token = user.EmailConfirmationToken;
            var encodedToken = Uri.EscapeDataString(token!);
            string verifyUrl = $"https://e-shop-roan-eight.vercel.app/verify-email/{encodedToken}";
            // Gerçek kullanıcılara e-posta göndermek istiyorsan önce Resend’de bir domain (örneğin myshop.com) doğrulaman gerekiyor.
            //Ardından From adresini "MyShop <noreply@myshop.com>" gibi yaparsan artık resend.dev kısıtlaması kalkar ve istediğin adrese mail atabilirsin.
            var resp = await resend.EmailSendAsync(new EmailMessage()
            {
                From = "Acme <onboarding@resend.dev>",
                To = user.Email,
                Subject = "MailConfirm",
                HtmlBody = $@"
             <h2>Welcome, {user.UserName}!</h2>
        <p>Please confirm your account by clicking the button below 👇</p>
        <a href='{verifyUrl}'
           style='background:#4CAF50;color:white;padding:10px 20px;border-radius:5px;text-decoration:none;display:inline-block;'>
           Verify My Email
        </a>
        <p style='margin-top:15px;font-size:13px;color:#666;'>If you didn’t create an account, you can safely ignore this email.</p>",
            });
        }

        // [HttpGet("verify-email")]
        // public async Task<ActionResult> ConfirmMail([FromQuery] string token)
        [HttpGet("verify-email/{token}")]
public async Task<ActionResult> ConfirmMail([FromRoute] string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                    return BadRequest("token not found.");

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.EmailConfirmationToken != null && u.EmailConfirmationToken == token);

                if (user == null) return NotFound("Invalid or expired  token.");

                if (user.IsEmailConfirmed) return Ok("Mail already confirmed.");

                user.IsEmailConfirmed = true;
                user.EmailConfirmationToken = null;
                await _context.SaveChangesAsync();


                return Ok("E-posta doğrulandı!");
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Doğrulama hatası: {ex.Message}");
                return StatusCode(500, $"Server error: {ex.Message}");
            }

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
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> LogoutAsync()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

            var userId = int.Parse(userIdStr);
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
            if (string.IsNullOrEmpty(request.refreshtoken))
            {
                return Unauthorized("Invalid refresh token");
            }


            var user = await ValidateRefreshTokenAsync(request.UserId, request.refreshtoken);
            if (user == null)
            {
                return Unauthorized("Invalid or expired refresh token");
            }
            return await CreateTokenResponse(user);
        }
        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string email, string token)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return NotFound("User not found");
            }
            if (user.EmailConfirmationToken != token)
            {
                return BadRequest("Invalid token");
            }
            user.IsEmailConfirmed = true;
            user.EmailConfirmationToken = null;

            await _context.SaveChangesAsync();
            return Ok("Email confirm is succes");
        }
        [HttpGet("protectedRoute")]
        [Authorize()]
        public IActionResult TestRoute()
        {
            return Ok("Test success");
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