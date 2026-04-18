using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlindMatchAPI.Data;
using BlindMatchAPI.DTOs;
using BlindMatchAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace BlindMatchAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    
    // [Authorize(Roles = "ModuleLeader")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        // Constructor 
        public AdminController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: api/admin/users
       
        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<UserViewDTO>>> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new UserViewDTO
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    UserName = u.UserName,
                    Email = u.Email,
                    Role = u.Role
                })
                .ToListAsync();

            return Ok(users);
        }

        // POST: api/admin/add-user
       
        [HttpPost("add-user")]
        public async Task<IActionResult> AddUser([FromBody] RegisternowDTO model)
        {
            if (model == null)
            {
                return BadRequest("Invalid client request");
            }

            // 1. User Object 
            var user = new ApplicationUser
            {
                FullName = model.FullName,
                UserName = model.Email, 
                Email = model.Email,
                Role = model.Role
            };

            // 2. Password 
            
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                return Ok(new { message = "User created successfully!" });
            }

           
            return BadRequest(result.Errors);
        }

        // DELETE: api/admin/delete-user/{id}
        [HttpDelete("delete-user/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            // 1. Database 
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound(new { message = "User not found!" });
            }

           
            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                return Ok(new { message = "User deleted successfully!" });
            }

            return BadRequest(result.Errors);
        }
    }
}