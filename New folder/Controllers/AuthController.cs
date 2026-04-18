using BlindMatchAPI.DTOs;
using BlindMatchAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BlindMatchAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
        {
            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                return BadRequest("User with this email already exists.");

            // Validate role
            var allowedRoles = new[] { "Student", "Supervisor", "ModuleLeader" };
            if (!allowedRoles.Contains(dto.Role))
                return BadRequest("Invalid role. Must be Student, Supervisor, or ModuleLeader.");

            // Create new user
            var user = new ApplicationUser
            {
                FullName = dto.FullName,
                Email = dto.Email,
                UserName = dto.Email,
                Role = dto.Role
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // Create role if it doesn't exist and assign it
            if (!await _roleManager.RoleExistsAsync(dto.Role))
                await _roleManager.CreateAsync(new IdentityRole(dto.Role));

            await _userManager.AddToRoleAsync(user, dto.Role);

            return Ok("User registered successfully.");
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            // Find user by email
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return Unauthorized("Invalid email or password.");

            // Check password
            var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!passwordValid)
                return Unauthorized("Invalid email or password.");

            // Get user roles
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Student";

            // Generate JWT token
            var token = GenerateJwtToken(user, role);

            return Ok(new AuthResponseDTO
            {
                Token = token,
                FullName = user.FullName,
                Email = user.Email!,
                Role = role
            });
        }

        // POST: api/auth/setup-admin (run once to create admin)
        [HttpPost("setup-admin")]
        public async Task<IActionResult> SetupAdmin()
        {
            var adminEmail = "admin@blindmatch.com";
            var existing = await _userManager.FindByEmailAsync(adminEmail);
            if (existing != null)
                return BadRequest("Admin already exists.");

            var admin = new ApplicationUser
            {
                FullName = "Module Leader",
                Email = adminEmail,
                UserName = adminEmail,
                Role = "ModuleLeader"
            };

            var result = await _userManager.CreateAsync(admin, "Admin123!");
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            if (!await _roleManager.RoleExistsAsync("ModuleLeader"))
                await _roleManager.CreateAsync(new IdentityRole("ModuleLeader"));

            await _userManager.AddToRoleAsync(admin, "ModuleLeader");
            return Ok("Admin created. Email: admin@blindmatch.com / Password: Admin123!");
        }

        // Helper: Generate JWT Token
        private string GenerateJwtToken(ApplicationUser user, string role)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, role)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(
                    double.Parse(_configuration["Jwt:ExpiryHours"]!)),
                signingCredentials: new SigningCredentials(
                    key, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}