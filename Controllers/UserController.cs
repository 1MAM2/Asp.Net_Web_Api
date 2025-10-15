using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using productApi.Context;
using productApi.DTOS.UserDTOs;
using productApi.Models;

namespace productApi.Controllers
{
    [ApiController]
    [Authorize(Roles = "Customer,Admin")]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly productDb _context;
        public UserController(productDb context)
        {
            _context = context;
        }
        [HttpGet()]
        public async Task<ActionResult<UserReadDTO>> GetAllUsers()
        {
            var users = await _context.Users.ToListAsync();

            var userDTO = users.Select(user => new UserReadDTO
            {
                Id = user.Id,
                UserName = user.UserName,
                Address = user.Address,
                Email = user.Email,
                Role = user.Role,
            }).ToList();
            return Ok(userDTO);
        }

        [HttpGet("me")]
        public async Task<ActionResult<UserReadDTO>> OneUserAllData()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {

                var id = int.Parse(userId);
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound("User not found pls try again later");
                }
                var sendUser = new User()
                {
                    UserName = user.UserName,
                    Role = user.Role,
                    Address = user.Address,
                    Email = user.Email,
                };
                return Ok(sendUser);
            }
            return NotFound("User not found");
        }
        [HttpDelete("deleteAccount")]
        public async Task<IActionResult> SoftDeleteAccount()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out var userId))
            {
                return BadRequest("Invalid user ID");
            }
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("Account not found");

            user.IsDeleted = true;
            await _context.SaveChangesAsync();
            return Ok();
        }
        [HttpPut("updateuser")]
        public async Task<ActionResult> UpdateAsyncUser(UserUpdateDTO req)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out var userId))
            {
                return BadRequest("Invalid user ID");
            }
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found");


            user.UserName = req.UserName;
            user.Address = req.Address;
            user.Email = req.Email;


            await _context.SaveChangesAsync();
            return Ok("User update success");
        }
        [HttpPut("change-role")]
        public async Task<IActionResult> ChangeUserRole(int id, string role)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("User not found");
            user.Role = role;
            await _context.SaveChangesAsync();
            return Ok("Role changed");
        }
    }
}