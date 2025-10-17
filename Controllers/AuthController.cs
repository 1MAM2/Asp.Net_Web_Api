using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Resend;
using System.Net;
using productApi.Context;
using productApi.DTOS.UserDTOs;
using productApi.Models;

namespace productApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly productDb _context;
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _config;
        private readonly string _resendApiKey;

        public AuthController(productDb context, IConfiguration config, ILogger<AuthController> logger)
        {
            _context = context;
            _config = config;
            _logger = logger;
            _resendApiKey = _config.GetValue<string>("Resend:ApiKey") ?? string.Empty;
        }

        [HttpPost("register")]
        public async Task<ActionResult> RegisterAsync([FromBody] UserRegisterDTO request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // email ve username için case-insensitive kontrol
            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower(), ct))
                return BadRequest(new { message = "Email already exists" });

            if (await _context.Users.AnyAsync(u => u.UserName.ToLower() == request.UserName.ToLower(), ct))
                return BadRequest(new { message = "You already have an account" });

            var hasher = new PasswordHasher<User>();
            var newUser = new User
            {
                UserName = request.UserName,
                Role = string.IsNullOrWhiteSpace(request.Role) ? "Customer" : request.Role,
                Address = request.Address,
                Email = request.Email,
                EmailConfirmationToken = Guid.NewGuid().ToString(),
                PhoneNumber = request.PhoneNumber,
                CreatedAt = DateTime.UtcNow // model'da varsa ekleyin
            };

            newUser.PasswordHash = hasher.HashPassword(newUser, request.Password);

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync(ct);

            // E-posta göndermeyi asenkron, hataları loglayarak başlat
            _ = SendVerificationEmailAsync(newUser, ct);

            // Created response (201)
            return CreatedAtAction(nameof(GetUserById), new { id = newUser.Id }, new { newUser.UserName, newUser.Id });
        }

        // basit get user by id örneği (CreatedAtAction için)
        [HttpGet("{id}")]
        public async Task<ActionResult<UserReadDTO>> GetUserById(int id, CancellationToken ct)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == id)
                .Select(u => new UserReadDTO { Id = u.Id, UserName = u.UserName, Email = u.Email })
                .FirstOrDefaultAsync(ct);

            if (user == null) return NotFound();
            return Ok(user);
        }

        // Bu bir HTTP endpoint değil, register sonrası çağrılan private metod.
        private async Task SendVerificationEmailAsync(User user, CancellationToken ct)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_resendApiKey))
                {
                    _logger.LogWarning("Resend API key is not configured. Skipping verification email.");
                    return;
                }

                var resend = ResendClient.Create(_resendApiKey);
                var token = user.EmailConfirmationToken;
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("User does not have an email confirmation token - skipping email.");
                    return;
                }

                string encodedToken = WebUtility.UrlEncode(token);
                string verifyUrl = $"{_config.GetValue<string>("Frontend:BaseUrl")?.TrimEnd('/')}/verify-email/{encodedToken}";

                var message = new EmailMessage()
                {
                    From = _config.GetValue<string>("Resend:From") ?? "Acme <noreply@yourdomain.com>",
                    To = user.Email,
                    Subject = "Please confirm your email",
                    HtmlBody = @$"
                        <h2>Welcome, {WebUtility.HtmlEncode(user.UserName)}!</h2>
                        <p>Please confirm your account by clicking the button below 👇</p>
                        <a href='{verifyUrl}'
                           style='background:#4CAF50;color:white;padding:10px 20px;border-radius:5px;text-decoration:none;display:inline-block;'>
                           Verify My Email
                        </a>
                        <p style='margin-top:15px;font-size:13px;color:#666;'>If you didn’t create an account, ignore this email.</p>"
                };

                await resend.EmailSendAsync(message, ct);
                _logger.LogInformation("Verification email queued/sent for user {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send verification email to user {UserId}", user.Id);
                // Hata burada yakalanıp loglandı; register akışını bozmak istemiyoruz.
            }
        }

        [HttpGet("verify-email")]
        public async Task<ActionResult> ConfirmMail([FromQuery] string token, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(token)) return BadRequest("token not found.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.EmailConfirmationToken != null && u.EmailConfirmationToken == token, ct);
            if (user == null) return NotFound("Invalid or expired token.");

            if (user.IsEmailConfirmed) return Ok("Mail already confirmed.");

            user.IsEmailConfirmed = true;
            user.EmailConfirmationToken = null;
            await _context.SaveChangesAsync(ct);

            return Ok("E-posta doğrulandı!");
        }

        [HttpPost("login")]
        public async Task<ActionResult<TokenResponseDTO>> LoginAsync([FromBody] UserDTO request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // kullanıcıyı case-insensitive bulma (kayıtlı kullanıcılar küçük/ büyük farklı olabilir)
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName.ToLower() == request.UserName.ToLower(), ct);
            if (user == null) return BadRequest("Account not found");

            var verifyResult = new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (verifyResult == PasswordVerificationResult.Failed) return Unauthorized("Username or password wrong");

            if (!user.IsEmailConfirmed)
            {
                // opsiyonel: eğer email doğrulama zorunluysa
                return Unauthorized("Please confirm your email before logging in.");
            }

            var response = await CreateTokenResponse(user, ct);
            return Ok(response);
        }

        [HttpPost("logout")]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> LogoutAsync(CancellationToken ct)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var user = await _context.Users.FindAsync(new object[] { userId }, ct);
            if (user == null) return NotFound();

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = DateTime.MinValue;
            await _context.SaveChangesAsync(ct);

            return Ok(new { message = "Logout success" });
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<ActionResult<TokenResponseDTO>> RefreshTokenAsync([FromBody] RefreshTokenRequestDTO request, CancellationToken ct)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.refreshToken))
                return Unauthorized("Invalid refresh token");

            // refresh token'ı DB'de arama (read-only önce)
            var userFromDb = await _context.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == request.refreshToken, ct);

            if (userFromDb == null)
                return Unauthorized("Invalid or expired refresh token");

            // validate token (ayrıca expiry kontrolü)
            if (userFromDb.RefreshTokenExpiryTime <= DateTime.UtcNow)
                return Unauthorized("Refresh token expired");

            // opsiyonel: burada daha güvenli olması için refresh token'ı DB'de hash'lenmiş tutman önerilir.
            // Eğer istersen ben bunun için örnek bir implementasyon ekleyebilirim.

            var newResponse = await CreateTokenResponse(userFromDb, ct);
            return Ok(newResponse);
        }

        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string email, [FromQuery] string token, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
                return BadRequest("Email and token are required");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), ct);
            if (user == null) return NotFound("User not found");

            if (user.EmailConfirmationToken != token) return BadRequest("Invalid token");

            user.IsEmailConfirmed = true;
            user.EmailConfirmationToken = null;
            await _context.SaveChangesAsync(ct);

            return Ok("Email confirm is success");
        }

        [HttpGet("protectedRoute")]
        [Authorize]
        public IActionResult TestRoute()
        {
            return Ok("Test success");
        }

        private async Task<TokenResponseDTO> CreateTokenResponse(User user, CancellationToken ct)
        {
            var refreshToken = await GenerateAndSaveRefreshTokenAsync(user, ct);
            return new TokenResponseDTO
            {
                accessToken = CreateToken(user),
                refreshToken = refreshToken,
                UserId = user.Id
            };
        }

        private string CreateToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName ),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role ?? "Customer"),
            };

            var keyString = _config.GetValue<string>("AppSettings:Token");
            if (string.IsNullOrEmpty(keyString)) throw new InvalidOperationException("JWT signing key is not configured.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

            var tokenDescriptor = new JwtSecurityToken(
                issuer: _config.GetValue<string>("AppSettings:Issuer"),
                audience: _config.GetValue<string>("AppSettings:Audience"),
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_config.GetValue<int>("AppSettings:AccessTokenExpiryMinutes", 15)),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private async Task<string> GenerateAndSaveRefreshTokenAsync(User user, CancellationToken ct)
        {
            var refreshToken = GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_config.GetValue<int>("AppSettings:RefreshTokenExpiryDays", 7));
            await _context.SaveChangesAsync(ct);
            return refreshToken;
        }
    }

}
