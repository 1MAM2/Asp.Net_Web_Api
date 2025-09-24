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
    //[Authorize(Roles ="Customer")]
    [Route("api/[controller]")]
    public class UserControler : ControllerBase
    {
        private readonly productDb _context;
        public UserControler(productDb context)
        {
            _context = context;
        }


        [HttpGet("/me")]
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
        [HttpDelete("/deleteAccount")]
        public async Task<IActionResult> SoftDeleteAccount(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("Account not found");

            user.IsDeleted = true;
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}