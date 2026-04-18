using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlindMatchAPI.Data;
using BlindMatchAPI.Models;
using Microsoft.AspNetCore.Authorization;

namespace BlindMatchAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                
                var studentCount = await _context.Users
                    .CountAsync(u => u.Role.ToLower() == "student");

                
                var supervisorCount = await _context.Users
                    .CountAsync(u => u.Role.ToLower() == "supervisor");

                var researchAreaCount = await _context.ResearchAreas.CountAsync();

                
                var proposalCount = await _context.Projects.CountAsync();

                
                var activeMatches = await _context.Matches.CountAsync();

                return Ok(new
                {
                    proposals = proposalCount,
                    activeMatches = activeMatches,
                    students = studentCount,
                    supervisors = supervisorCount,
                    researchAreas = researchAreaCount
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}